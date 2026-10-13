import json, hashlib
from pathlib import Path
from datetime import datetime, timezone, timedelta
from email.message import EmailMessage
from email import policy
from email.parser import BytesParser

root = Path(__file__).resolve().parents[6]
run_dir = Path(__file__).resolve().parent
rel = lambda p: str(p.relative_to(root)).replace('\\', '/')
def read(p):
    return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p, data):
    p.parent.mkdir(parents=True, exist_ok=True)
    temp = p.with_suffix(p.suffix + '.tmp')
    temp.write_text(json.dumps(data, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    temp.replace(p)

data = read(run_dir / 'run-input.json')
state_path = root / 'docs/market/us/daily/STATE.json'
pipeline_path = root / 'docs/market/acquisition/PIPELINE.json'
partner_path = root / 'docs/market/acquisition/PARTNER-RESEARCH.json'
state, pipeline, partners = map(read, [state_path, pipeline_path, partner_path])
assert state['campaignStatus'] == 'active'
assert len(state['previouslyContacted']) == 55
assert not any(r.get('runId') == data['runId'] for r in state.get('scheduledRuns', []))
assert not state.get('activeSendingRun')
assert not (root / 'docs/market/us/daily/SEND-LOCK.json').exists()
assert data['evidence']['completion']['complete'] and not data['evidence']['completion']['newReplies']
original_contacts = json.dumps(state['previouslyContacted'], sort_keys=True)
original_hourly = json.dumps(state.get('hourlyRuns'), sort_keys=True)
now = datetime.now(timezone.utc)
stamp = now.isoformat().replace('+00:00', 'Z')
local = now.astimezone(timezone(timedelta(hours=1)))
assert local.date().isoformat() == data['date'], 'Date changed'
manifests = sorted((root / 'docs/market').rglob('campaign.json'))
manifest_hashes = {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in manifests}
pending, confirmed = [], []
for p in manifests:
    for m in read(p).get('messages', []):
        if m.get('status') in ('sending', 'unknown', 'send_unknown'):
            pending.append(m)
        if m.get('status') == 'sent' and m.get('messageId'):
            confirmed.append(m)
assert not pending
assert len({m['messageId'] for m in confirmed}) == 55
sent_ids = set(data['evidence']['initial']['searches'][5]['ids'])
attachment_ids = set(data['evidence']['initial']['searches'][6]['ids'])
assert all(c['messageId'] in sent_ids and c['messageId'] in attachment_ids for c in state['previouslyContacted'])
sent_by_id = {s['id']: s for t in data['evidence']['initial']['conversations'] for s in t['sent']}
for c in state['previouslyContacted']:
    s = sent_by_id[c['messageId']]
    headers = {h['name'].lower(): h['value'] for h in s['headers']}
    assert c['email'].lower() in headers['to'].lower()
    assert headers['subject'] and headers['from'] == 'ramishqureshi99@gmail.com'
    assert s['video'] == [{'filename': 'nexora-product-demo-male.mp4', 'mime': 'video/mp4', 'size': 1945313}]
video = root / 'docs/market/pitch/nexora-product-demo-male.mp4'
video_bytes = video.read_bytes()
video_hash = hashlib.sha256(video_bytes).hexdigest().upper()
assert len(video_bytes) == state['attachment']['size_bytes']
assert video_hash == state['attachment']['sha256'].upper()
recent = [c for c in state['previouslyContacted'] if c.get('sentAt') and now - datetime.fromisoformat(c['sentAt'].replace('Z', '+00:00')) < timedelta(hours=5)]
today = [c for c in state['previouslyContacted'] if c.get('localDate') == data['date']]
assert not recent and not today
suppression_path = root / 'docs/market/OUTREACH-SUPPRESSIONS.json'
suppression_hash = hashlib.sha256(suppression_path.read_bytes()).hexdigest()
suppression = read(suppression_path)
for candidate in data['candidates']:
    assert not any(c.get('domain') in candidate['domains'] or c['organisation'].casefold() == candidate['organisation'].casefold() for c in state['previouslyContacted'])
    assert not any(c.get('domain') in candidate['domains'] or (candidate.get('buyerContact') and c.get('email') == candidate['buyerContact']) for c in suppression['entries'])

write(run_dir / 'reply-and-sent-checks.json', data['evidence']['initial'])
write(run_dir / 'completion-checks.json', data['evidence']['completion'])
write(run_dir / 'candidate-duplicate-checks.json', data['evidence']['candidateDuplicates'])
drafts = data['drafts']
draft_verification = []
for index, draft in enumerate(drafts):
    slug = 'us-rega' if index == 0 else 'uae-sharpminds'
    (run_dir / (slug + '-held.txt')).write_text('HELD LOCAL PITCH - not sent.\nSubject: ' + draft['subject'] + '\nTo: ' + str(draft['to']) + '\n\n' + draft['body'] + '\n', encoding='utf-8')
    (run_dir / (slug + '-held.html')).write_text(draft['html'], encoding='utf-8')
    if draft['to']:
        message = EmailMessage(policy=policy.SMTP)
        for name, value in [('From', draft['from']), ('Reply-To', draft['replyTo']), ('To', draft['to']), ('Subject', draft['subject'])]:
            message[name] = value
        message.set_content(draft['body'])
        message.add_alternative(draft['html'], subtype='html')
        message.add_attachment(video_bytes, maintype='video', subtype='mp4', filename=video.name)
        raw = message.as_bytes()
        parsed = BytesParser(policy=policy.default).parsebytes(raw)
        attachments = list(parsed.iter_attachments())
        assert parsed['To'] == draft['to'] and not parsed.get('Cc') and not parsed.get('Bcc')
        assert parsed['From'] == parsed['Reply-To'] == 'ramishqureshi99@gmail.com'
        assert parsed.get_content_type() == 'multipart/mixed'
        assert parsed.get_body(('plain',)) and parsed.get_body(('html',))
        assert len(attachments) == 1
        actual = attachments[0].get_payload(decode=True)
        assert attachments[0].get_filename() == video.name and attachments[0].get_content_type() == 'video/mp4'
        assert len(actual) == len(video_bytes) and hashlib.sha256(actual).hexdigest().upper() == video_hash
        eml_path = run_dir / (slug + '-held.eml')
        eml_path.write_bytes(raw)
        draft['mimeFile'] = rel(eml_path)
        draft['attachmentEmbedded'] = True
        draft_verification.append({'organisation': draft['organisation'], 'recipient': draft['to'], 'MIMEVerified': True, 'attachmentHashVerified': True, 'bytes': len(raw), 'sent': False})
    else:
        draft['attachmentEmbedded'] = False
        draft['mimeFile'] = None
        draft_verification.append({'organisation': draft['organisation'], 'recipient': None, 'MIMEVerified': False, 'reason': 'Held body only; published recipient not verified', 'sent': False})
write(run_dir / 'held-drafts.json', drafts)
write(run_dir / 'draft-verification.json', draft_verification)

markets = {}
for candidate in data['candidates']:
    regional = 'us' if candidate['market'] == 'United States' else 'middle-east'
    research_dir = root / 'docs/market' / regional / 'research' / data['date'] / '062819Z'
    candidate['localLedgerDuplicateCheck'] = {'complete': True, 'manifestCount': len(manifests), 'confirmedHistory': 55, 'matches': []}
    candidate['preSendChecks'] = {'replyChecksComplete': True, 'liveSentAndDuplicateChecksComplete': True, 'at': data['evidence']['completion']['at'], 'sendPermissionAvailable': False}
    write(research_dir / 'qualification.json', candidate)
    notes = ['# ' + candidate['organisation'] + ' - held qualification', '', 'Checked: ' + candidate['checkedAt'], 'Offer: website/CMS; need is a hypothesis, not buyer-confirmed.', '', candidate['observedFact'], '', 'Sources:']
    for e in candidate['needEvidence'] + candidate.get('buyerContactEvidence', []) + candidate.get('providerEvidence', []) + candidate.get('countryRulesEvidence', []):
        notes.append('- ' + e['url'] + ' | source date: ' + str(e.get('sourceDate')) + ' | ' + e.get('observation', e.get('verification', '')))
    notes += ['', 'Holds:'] + ['- ' + h for h in candidate['holdReasons']] + ['', 'Buyer interest, dissatisfaction, budget and procurement timing remain unknown. Existing/unknown CRM is informational for CMS. No systems replacement or integration readiness promised.', '', 'Next action: ' + candidate['nextAction']]
    (research_dir / 'sources.md').write_text('\n'.join(notes) + '\n', encoding='utf-8')
    qp = root / 'docs/market' / regional / 'research/CRM-QUALIFICATION.json'
    qualification = read(qp)
    assert not any(c.get('domain') == candidate['domain'] for c in qualification.get('records', []))
    qualification.setdefault('records', []).append(candidate)
    qualification.setdefault('qualificationRuns', []).append({'runId': data['runId'], 'at': stamp, 'candidate': candidate['organisation'], 'offerType': 'website_cms', 'sendEligible': False, 'decision': candidate['outreachDecision']})
    qualification['updatedAt'] = stamp
    write(qp, qualification)
    assert not any(c.get('domain') == candidate['domain'] for c in pipeline['opportunities'])
    pipeline['opportunities'].append(candidate)
    markets[candidate['market']] = {'status': candidate['stage'], 'freshResearch': candidate['organisation'], 'holdReasons': candidate['holdReasons'], 'qualification': rel(research_dir / 'qualification.json'), 'attemptedRecipient': None, 'preparedRecipient': candidate.get('buyerContact'), 'messageIds': []}

assert not any(p.get('domain') == data['partner']['domain'] for p in partners['candidates'])
partners['candidates'].append(data['partner'])
partners['updatedAt'] = stamp
write(partner_path, partners)
pipeline['lastRunAt'] = stamp
write(pipeline_path, pipeline)
record = {'runId': data['runId'], 'startedAt': data['startedAt'], 'completedAt': stamp, 'localDate': data['date'], 'status': 'held_sender_approval', 'sentCount': 0, 'sendToolCalls': 0, 'confirmedMessageIds': [], 'markets': markets, 'manifest': None, 'drafts': rel(run_dir / 'held-drafts.json'), 'report': rel(run_dir / 'run-report.json'), 'verification': {'initialReplyComplete': True, 'completionReplyComplete': True, 'priorExternalIdsVerified': 55, 'liveSentCountIncludingSelfTest': 56, 'newPositiveReplies': 0, 'newDeliveryFailures': 0, 'candidateDuplicatesComplete': True}}
state.setdefault('scheduledRuns', []).append(record)
state['lastReplyCheckAt'] = data['evidence']['completion']['at']
state['lastReplyCheckComplete'] = True
state['lastSuccessfulReplyCheckAt'] = data['evidence']['completion']['at']
state['lastReplyCheckEvidence'] = rel(run_dir / 'reply-and-sent-checks.json')
state['lastReplyCompletionEvidence'] = rel(run_dir / 'completion-checks.json')
state['lastReplyCheckFailure'] = None
state['lastCampaignRunAt'] = stamp
state['senderBlock']['lastPolicyObservedAt'] = stamp
state['lastGmailReadRecoveryAt'] = data['evidence']['completion']['at']
assert original_contacts == json.dumps(state['previouslyContacted'], sort_keys=True)
assert original_hourly == json.dumps(state.get('hourlyRuns'), sort_keys=True)
write(state_path, state)
report = {**record, 'automationId': 'nexora-daily-us-sales-outreach', 'timezone': 'Europe/London', 'localStartedAt': '2026-10-12T07:28:19.133+01:00', 'localCompletedAt': local.isoformat(), 'attachment': {'path': str(video), 'filename': video.name, 'sizeBytes': len(video_bytes), 'sha256': video_hash, 'matchesState': True}, 'draftVerification': draft_verification, 'localGuards': {'manifestCount': len(manifests), 'pendingSendingUnknownOutcomes': 0, 'senderLockAbsent': True, 'activeSendingRun': None, 'rollingFiveHourConfirmedSends': len(recent), 'confirmedToday': len(today), 'perMarketLimit': 1, 'totalIntervalLimit': 2, 'dailyLimit': 24}, 'senderBlock': {'reason': state['senderBlock']['reason'], 'alreadyNotified': True, 'sendCallRetried': False, 'reasonNotRetried': 'Known approval policy restriction; no configuration change. Read recovery does not imply send permission.'}, 'weeklyReview': {'performed': False, 'lastDate': pipeline['lastWeeklyCohortReviewDate'], 'nextDue': pipeline['nextWeeklyCohortReviewDate']}, 'proofDeliverables': [{'asset': 'Dedicated CMS demonstration', 'status': 'outstanding', 'nextAction': 'Verify editor draft/preview/publication of three sample pages'}, {'asset': 'Dedicated supported portal demonstration', 'status': 'outstanding', 'nextAction': 'Verify granted private-page/profile access and denial/revocation'}, {'asset': 'Truthful public product page', 'status': 'outstanding_not_published', 'nextAction': 'Prepare accurate module outcomes and real founder/contact details; no deployment enabled'}], 'partnerResearch': data['partner'], 'referralPreparation': {'template': 'docs/market/acquisition/INTRODUCTION-REQUEST.md', 'actualFounderRelationships': 'None provided/verified', 'specificIntroductionPrepared': False, 'messagesSent': 0}, 'nextAction': 'Enable Gmail send approval in the authorised session. Before any send repeat full replies, outcomes, suppression, duplicate and cap checks; ME also needs verified published recipient and specific lawful channel route.', 'notification': 'Initial and completion Gmail checks have recovered; no new positive response. Send-approval block remains unchanged.', 'changes': 'Market documents/state/pipeline/local drafts only; no application or sibling edits, external messages, deployment, purchases, commit or push.'}
write(run_dir / 'run-report.json', report)
(run_dir / 'run-report.md').write_text('# Dual-market run: 12 October 2026\n\n' + local.strftime('%H:%M') + ' Europe/London. Zero emails sent; zero send calls.\n\nInitial and completion live Gmail checks succeeded, including domain searches, overlap, spam/trash, actual reply bodies, 55 associated conversations, prior Sent IDs and MP4 metadata. No new positive reply, opt-out or delivery failure was found. Known BAFSA organisation suppression and MPIF address-only retirement block remain.\n\nUS: REGA Engineering Group - verified 2026 Omaha expansion and public general inbox; local MIME pitch contains text/HTML and verified MP4, held for sender approval. Existing developer credit retained without inferring an unsatisfied buyer or missing provider. UAE: SharpMinds - dated July Dubai studio announcement; held body only because exact email and recipient-specific lawful route remain unverified. UAE official legislation fetch failed; no blanket exemption or restriction invented.\n\nBoth slots remain unused. All 55 confirmed external sends and four manifests preserved. Next weekly review: 17 October. Dedicated CMS/portal videos and public product page remain outstanding. True North Technologies added only to the partner research queue; no relationship, willingness or introduction invented.\n', encoding='utf-8')
assert all(hashlib.sha256(Path(p).read_bytes()).hexdigest() == expected for p, expected in manifest_hashes.items())
assert hashlib.sha256(suppression_path.read_bytes()).hexdigest() == suppression_hash
assert len(read(state_path)['previouslyContacted']) == 55
print(json.dumps({'runId': data['runId'], 'replyChecksComplete': True, 'positiveReplies': 0, 'newSends': 0, 'sendCalls': 0, 'pipelineCount': len(pipeline['opportunities']), 'priorSentCountPreserved': 55, 'manifestCountPreserved': len(manifests), 'USMimeVerified': True, 'MEHeldWithoutRecipient': True, 'nextWeeklyReview': pipeline['nextWeeklyCohortReviewDate'], 'report': rel(run_dir / 'run-report.json')}))
