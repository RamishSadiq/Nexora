"""Persist this interval-held four-country run; no email sending capability."""
import json, hashlib
from pathlib import Path
from datetime import datetime, timezone, timedelta
from email.message import EmailMessage
from email.parser import BytesParser
from email import policy

root = Path(__file__).resolve().parents[6]
run_dir = Path(__file__).resolve().parent
rel = lambda p: str(p.relative_to(root)).replace('\\', '/')
def read(p):
    return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p, value):
    p.parent.mkdir(parents=True, exist_ok=True)
    temp = p.with_suffix(p.suffix + '.tmp')
    temp.write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    temp.replace(p)

data = read(run_dir / 'run-input.json')
sp = root / 'docs/market/us/daily/STATE.json'
pp = root / 'docs/market/acquisition/PIPELINE.json'
state, pipeline = read(sp), read(pp)
markets = ['Australia', 'United States', 'New Zealand', 'Canada']
assert state['campaignStatus'] == 'active' and state['activeMarkets'] == markets
assert state['senderBlock'].get('active') is False and state['senderBlock'].get('runtimeSendVerified') is True
assert len(state['previouslyContacted']) == 59
assert not state.get('activeSendingRun') and not (root / 'docs/market/us/daily/SEND-LOCK.json').exists()
assert not any(r.get('runId') == data['runId'] for r in state.get('scheduledRuns', []))
assert data['evidence']['initial']['complete'] and data['evidence']['completion']['complete']
assert not data['evidence']['completion']['newReplies']
now = datetime.now(timezone.utc)
stamp = now.isoformat().replace('+00:00', 'Z')
local = now.astimezone(timezone(timedelta(hours=1)))
assert local.date().isoformat() == data['date']
original_contacts = json.dumps(state['previouslyContacted'], sort_keys=True)
original_hourly = json.dumps(state.get('hourlyRuns'), sort_keys=True)
original_sender_block = json.dumps(state['senderBlock'], sort_keys=True)
suppression_path = root / 'docs/market/OUTREACH-SUPPRESSIONS.json'
suppression_hash = hashlib.sha256(suppression_path.read_bytes()).hexdigest()
suppression = read(suppression_path)
manifest_paths = sorted((root / 'docs/market').rglob('campaign.json'))
manifest_hashes = {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in manifest_paths}
confirmed, pending = [], []
for p in manifest_paths:
    for m in read(p).get('messages', []):
        if m.get('status') == 'sent' and m.get('messageId'):
            confirmed.append(m)
        elif m.get('status') in ('sending', 'unknown', 'send_unknown'):
            pending.append(m)
assert not pending and len({m['messageId'] for m in confirmed}) == 59
recent = [c for c in state['previouslyContacted'] if c.get('sentAt') and datetime.fromisoformat(c['sentAt'].replace('Z', '+00:00')) > now - timedelta(hours=5)]
assert len(recent) == 4 and all(sum(c.get('market') == market for c in recent) == 1 for market in markets)
today = [c for c in state['previouslyContacted'] if c.get('localDate') == data['date']]
assert len(today) == 4
sent_ids = set(data['evidence']['initial']['searches'][-2]['ids'])
video_ids = set(data['evidence']['initial']['searches'][-1]['ids'])
assert all(c['messageId'] in sent_ids and c['messageId'] in video_ids for c in state['previouslyContacted'])
assert all(mid in sent_ids for mid in state['senderBlock']['confirmedRecoveryMessageIds'])
sent_by_id = {s['id']: s for t in data['evidence']['initial']['conversations'] for s in t['sent']}
for c in state['previouslyContacted']:
    m = sent_by_id[c['messageId']]
    h = {v['name'].lower(): v['value'] for v in m['headers']}
    assert c['email'].lower() in h['to'].lower() and h['subject'] and h['from'] == 'ramishqureshi99@gmail.com'
    assert m['video'] == [{'filename': 'nexora-product-demo-male.mp4', 'mime': 'video/mp4', 'size': 1945313}]
video_path = root / 'docs/market/pitch/nexora-product-demo-male.mp4'
video_bytes = video_path.read_bytes()
video_hash = hashlib.sha256(video_bytes).hexdigest().upper()
assert len(video_bytes) == state['attachment']['size_bytes'] and video_hash == state['attachment']['sha256'].upper()

write(run_dir / 'reply-and-sent-checks.json', data['evidence']['initial'])
write(run_dir / 'completion-checks.json', data['evidence']['completion'])
write(run_dir / 'candidate-duplicate-checks.json', data['evidence']['candidateDuplicates'])
outcomes, next_slots = {}, {}
for c in recent:
    eligible = datetime.fromisoformat(c['sentAt'].replace('Z', '+00:00')) + timedelta(hours=5)
    next_slots[c['market']] = {'eligibleAfterUTC': eligible.astimezone(timezone.utc).isoformat(), 'eligibleAfterLondon': eligible.astimezone(timezone(timedelta(hours=1))).isoformat(), 'consumedByMessageId': c['messageId']}

draft_verification = []
for draft in data['drafts']:
    slug = draft['market'].lower().replace(' ', '-')
    (run_dir / (slug + '-held.txt')).write_text('HELD PREPARATION - not sent.\nTo: ' + str(draft['to']) + '\nSubject: ' + draft['subject'] + '\n\n' + draft['body'] + '\n', encoding='utf-8')
    (run_dir / (slug + '-held.html')).write_text(draft['html'], encoding='utf-8')
    draft['mimeFile'] = None
    draft['attachmentEmbedded'] = False
    if draft['to']:
        message = EmailMessage(policy=policy.SMTP)
        for name, value in [('From', draft['from']), ('Reply-To', draft['replyTo']), ('To', draft['to']), ('Subject', draft['subject'])]:
            message[name] = value
        message.set_content(draft['body'])
        message.add_alternative(draft['html'], subtype='html')
        message.add_attachment(video_bytes, maintype='video', subtype='mp4', filename=video_path.name)
        raw = message.as_bytes()
        parsed = BytesParser(policy=policy.default).parsebytes(raw)
        attachment = list(parsed.iter_attachments())
        assert parsed['To'] == draft['to'] and not parsed.get('Cc') and not parsed.get('Bcc')
        assert parsed['From'] == parsed['Reply-To'] == 'ramishqureshi99@gmail.com'
        assert parsed.get_content_type() == 'multipart/mixed' and parsed.get_body(('plain',)) and parsed.get_body(('html',))
        assert len(attachment) == 1 and attachment[0].get_filename() == video_path.name and attachment[0].get_content_type() == 'video/mp4'
        attached = attachment[0].get_payload(decode=True)
        assert len(attached) == len(video_bytes) and hashlib.sha256(attached).hexdigest().upper() == video_hash
        eml = run_dir / (slug + '-held.eml')
        eml.write_bytes(raw)
        draft['mimeFile'] = rel(eml)
        draft['attachmentEmbedded'] = True
    draft_verification.append({'market': draft['market'], 'recipientSelected': bool(draft['to']), 'MIMEVerified': draft['attachmentEmbedded'], 'attachmentHashVerified': draft['attachmentEmbedded'], 'sent': False})
write(run_dir / 'held-drafts.json', data['drafts'])
write(run_dir / 'draft-verification.json', draft_verification)

for candidate in data['candidates']:
    assert not any(c.get('domain') in candidate['domains'] or c['organisation'].casefold() == candidate['organisation'].casefold() for c in state['previouslyContacted'])
    assert not any(s.get('domain') in candidate['domains'] or (candidate.get('buyerContact') and s.get('email') == candidate['buyerContact']) for s in suppression['entries'])
    candidate['intervalHold'] = next_slots[candidate['market']]
    candidate['localLedgerDuplicateCheck'] = {'complete': True, 'confirmedPriorIds': 59, 'manifestCount': len(manifest_paths), 'matches': []}
    candidate['preSendChecks'] = {'globalRepliesComplete': True, 'liveDuplicatesComplete': True, 'rollingCountrySlotsUsed': 1, 'sendPermissionHold': False, 'verifiedAt': data['evidence']['completion']['at']}
    existing = next((c for c in pipeline['opportunities'] if c.get('domain') == candidate['domain']), None)
    if existing:
        assert candidate['organisation'] == 'G&S Restaurant Equipment'
        history = existing.get('history', []) + candidate['history']
        previous_evidence = existing.get('needEvidence', [])
        protected = {k: existing[k] for k in ['buyerConfirmedProblem', 'budget', 'currency', 'decisionDate'] if existing.get(k) is not None}
        existing.update(candidate)
        existing.update(protected)
        existing['history'] = history
        existing['priorNeedEvidence'] = previous_evidence
    else:
        pipeline['opportunities'].append(candidate)
    outcomes[candidate['market']] = {'status': 'held_interval_cap', 'organisation': candidate['organisation'], 'additionalHoldReasons': candidate['holdReasons'][1:], 'preparedRecipient': candidate.get('buyerContact'), 'attemptedRecipient': None, 'messageIds': [], 'earliestEligibility': next_slots[candidate['market']]}

for opportunity in pipeline['opportunities']:
    if opportunity.get('stage') == 'held_sender_approval':
        opportunity['stage'] = 'needs_requalification'
        opportunity['sendEligible'] = False
        opportunity['holdReasons'] = [h for h in opportunity.get('holdReasons', []) if not any(word in h.lower() for word in ('sender approval', 'send-approval', 'approval policy', 'gmail send-approval'))]
        opportunity['holdReasons'].append('Prior approval hold resolved; requalify current sources/recipient and pre-send checks when due or selected')
        opportunity.setdefault('history', []).append({'at': stamp, 'event': 'Historical sender approval hold cleared under current four-country permission evidence; no site revisit or email sent.'})
pipeline['lastRunAt'] = stamp
write(pp, pipeline)
write(run_dir / 'candidates.json', data['candidates'])

record = {'runId': data['runId'], 'startedAt': data['startedAt'], 'completedAt': stamp, 'localDate': data['date'], 'status': 'skipped_rolling_interval_full', 'manifest': None, 'sentCount': 0, 'sendToolCalls': 0, 'confirmedMessageIds': [], 'markets': outcomes, 'verification': {'initialReplyComplete': True, 'completionReplyComplete': True, 'priorExternalIdsVerified': 59, 'liveSentCountIncludingSelfTest': 60, 'newPositiveReplies': 0, 'newDeliveryFailures': 0, 'candidateDuplicatesComplete': True}, 'drafts': rel(run_dir / 'held-drafts.json'), 'report': rel(run_dir / 'run-report.json')}
state.setdefault('scheduledRuns', []).append(record)
state['lastReplyCheckAt'] = data['evidence']['completion']['at']
state['lastSuccessfulReplyCheckAt'] = data['evidence']['completion']['at']
state['lastReplyCheckComplete'] = True
state['lastReplyCheckFailure'] = None
state['lastReplyCheckEvidence'] = rel(run_dir / 'reply-and-sent-checks.json')
state['lastReplyCompletionEvidence'] = rel(run_dir / 'completion-checks.json')
state['lastCampaignRunAt'] = stamp
assert original_contacts == json.dumps(state['previouslyContacted'], sort_keys=True)
assert original_hourly == json.dumps(state.get('hourlyRuns'), sort_keys=True)
assert original_sender_block == json.dumps(state['senderBlock'], sort_keys=True)
write(sp, state)

report = {**record, 'automationId': 'nexora-daily-us-sales-outreach', 'timezone': 'Europe/London', 'localStartedAt': '2026-10-12T17:23:29.573+01:00', 'localCompletedAt': local.isoformat(), 'currentPolicy': 'docs/market/MULTI-MARKET-CAMPAIGN.md', 'localGuards': {'manifestCount': len(manifest_paths), 'pendingUnknownOutcomes': 0, 'activeSendingRun': None, 'senderLockAbsent': True, 'confirmedToday': len(today), 'rollingFiveHourConfirmed': len(recent), 'perCountryLimit': 1, 'totalIntervalLimit': 4, 'dailyLimit': 24}, 'senderAccess': {'historicalBlockActive': False, 'currentSendAccessAlreadyVerified': True, 'basis': 'Four accepted messages earlier today reconciled in current live Sent; permission resolution preserved', 'restrictionBypass': False, 'additionalSendCallNeeded': False, 'reason': 'All four rolling interval slots already consumed; no legal send attempt available this run'}, 'attachment': {'filename': video_path.name, 'sizeBytes': len(video_bytes), 'sha256': video_hash, 'matchesState': True}, 'draftVerification': draft_verification, 'weeklyReview': {'performed': False, 'lastDate': pipeline['lastWeeklyCohortReviewDate'], 'nextDue': pipeline['nextWeeklyCohortReviewDate']}, 'proofDeliverables': ['Dedicated CMS demonstration outstanding', 'Dedicated supported-portal demonstration outstanding', 'Truthful public product page not yet published'], 'partnerResearch': {'existingQueuePreserved': True, 'founderRelationshipOrWillingness': 'None supplied/verified', 'specificIntroductionPrepared': False, 'messagesSent': 0}, 'newMiddleEastPitches': 0, 'nextAction': 'Continue at next scheduled run without catch-up; recheck replies/outcomes/caps. AU/NZ/Canada prospects need remaining contact/notice/relevance assessment; G&S can be requalified independently after US interval expiry.', 'notification': 'DONT_NOTIFY: no new send, reply or actionable failure; normal cap hold after verified earlier sends.'}
write(run_dir / 'run-report.json', report)
notes = ['# Four-country preparation: 12 October 2026', '', local.strftime('%H:%M') + ' Europe/London. No new sends: all four rolling slots consumed by earlier pitches.', '59 prior external IDs and 60 Sent including self-test reconciled. Replies/threads/delivery searches and completion checks complete; no new substantive replies.', '', 'The old approval block is resolved. No new Middle East contact. No unused-slot transfer or accumulated catch-up.', '']
for candidate in data['candidates']:
    notes += ['## ' + candidate['market'] + ': ' + candidate['organisation'], '', candidate['observedFact'], 'Problem hypothesis: ' + candidate['problemHypothesis'], 'Contact route: ' + candidate['outreachLegalBasis'], 'Next action: ' + candidate['nextAction'], '']
    notes += ['- ' + e['url'] + ' | source date: ' + str(e.get('sourceDate')) + ' | checked: ' + str(e.get('checkedAt')) for e in candidate['needEvidence'] + candidate.get('buyerContactEvidence', []) + candidate.get('countryRulesEvidence', [])]
    notes += ['', 'Linked refusal/privacy notices fully assessed: ' + str(candidate['publicationNoticesFullyInspected']), '']
(run_dir / 'sources.md').write_text('\n'.join(notes) + '\n', encoding='utf-8')
(run_dir / 'run-report.md').write_text('# Four-country run - 12 October 2026\n\n' + local.strftime('%H:%M') + ' Europe/London. No sends, no send calls. The four earlier pitches still consume one slot per country and all four slots in the rolling five-hour window. Earliest eligible times range from 19:49 to 19:51 London; these are cap expiry times, not additional scheduled send promises.\n\nGlobal live reply checks, actual bodies, associated conversations and Sent/MP4 reconciliation completed. No new interest, opt-out or delivery failure. Historical suppressions and 59 confirmed pitches preserved. Current Gmail send permission is verified by the earlier batch and its live Sent entries; the old blanket approval hold is not applied.\n\nPrepared MGAC (Australia), requalified G&S (US), North Projects (NZ) and Prism Engineering (Canada). The US local MIME pitch includes verified video; other copy is unaddressed preparation while recipient-specific contact/consent/notice checks remain incomplete. Existing CRM is informational for the CMS offer. No new Middle East pitches.\n\nWeekly review remains due 17 October. CMS/portal proof videos and public product page remain outstanding. Partner queue preserved without invented relationships or external messages.\n', encoding='utf-8')
assert all(hashlib.sha256(Path(p).read_bytes()).hexdigest() == h for p, h in manifest_hashes.items())
assert hashlib.sha256(suppression_path.read_bytes()).hexdigest() == suppression_hash
print(json.dumps({'runId': data['runId'], 'status': record['status'], 'newSends': 0, 'replyComplete': True, 'rollingCount': 4, 'confirmedHistory': 59, 'pipelineCount': len(pipeline['opportunities']), 'senderBlockActive': state['senderBlock']['active'], 'earliestEligibility': next_slots, 'report': record['report']}))
