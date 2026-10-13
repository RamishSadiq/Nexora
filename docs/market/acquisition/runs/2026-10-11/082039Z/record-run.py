import json, hashlib, html
from pathlib import Path
from datetime import datetime, timezone, timedelta

root = Path(__file__).resolve().parents[6]
run_dir = Path(__file__).resolve().parent
rel = lambda p: str(p.relative_to(root)).replace('\\', '/')
def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))
def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + '.tmp')
    temporary.write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    temporary.replace(path)

data = read(run_dir / 'run-input.json')
state_path = root / 'docs/market/us/daily/STATE.json'
pipeline_path = root / 'docs/market/acquisition/PIPELINE.json'
state = read(state_path)
pipeline = read(pipeline_path)
assert state.get('campaignStatus') == 'active', 'Campaign status changed; recheck before recording'
assert len(state['previouslyContacted']) == 55, 'Contact history changed; rerun checks'
assert not any(r.get('runId') == data['runId'] for r in state.get('scheduledRuns', [])), 'Run already recorded'
assert not state.get('activeSendingRun'), 'Another sender is active'
lock = root / 'docs/market/us/daily/SEND-LOCK.json'
assert not lock.exists(), 'Sender lock exists'
original_contacts = json.dumps(state['previouslyContacted'], sort_keys=True)
completed = datetime.now(timezone.utc)
stamp = completed.isoformat().replace('+00:00', 'Z')
local = completed.astimezone(timezone(timedelta(hours=1)))
assert local.date().isoformat() == '2026-10-11', 'Date changed; do not record stale daily window'

manifests = sorted((root / 'docs/market').rglob('campaign.json'))
confirmed = []
pending = []
for path in manifests:
    ledger = read(path)
    for message in ledger.get('messages', []):
        if message.get('status') in ('sending', 'unknown', 'send_unknown'):
            pending.append({'manifest': rel(path), 'message': message})
        if message.get('status') == 'sent' and message.get('messageId'):
            confirmed.append(message)
assert not pending, 'Unresolved send outcome appeared'
assert len(set(m['messageId'] for m in confirmed)) == 55, 'Manifest count changed'
recent = [c for c in state['previouslyContacted'] if c.get('sentAt') and completed - datetime.fromisoformat(c['sentAt'].replace('Z', '+00:00')) < timedelta(hours=5)]
today = [c for c in state['previouslyContacted'] if c.get('localDate') == '2026-10-11']
video = root / 'docs/market/pitch/nexora-product-demo-male.mp4'
video_size = video.stat().st_size
video_hash = hashlib.sha256(video.read_bytes()).hexdigest().upper()
assert video_size == state['attachment']['size_bytes']
assert video_hash == state['attachment']['sha256'].upper()
attachment = {'path': str(video), 'filename': video.name, 'sizeBytes': video_size, 'sha256': video_hash, 'matchesState': True}

write(run_dir / 'reply-and-sent-checks.json', data['evidence']['initial'])
write(run_dir / 'completion-checks.json', data['evidence']['completion'])
write(run_dir / 'held-drafts.json', data['drafts'])
for index, draft in enumerate(data['drafts']):
    slug = 'us-atlas' if index == 0 else 'uae-honeypot'
    (run_dir / (slug + '-held.txt')).write_text('HELD LOCAL BODY: no recipient selected; planned video is not embedded; no Gmail draft/send created.\nSubject: ' + draft['subject'] + '\n\n' + draft['body'] + '\n', encoding='utf-8')
    (run_dir / (slug + '-held.html')).write_text('<!doctype html><meta charset="utf-8"><title>Held local pitch</title><p><strong>Held; no recipient selected and no message sent.</strong></p>' + draft['html'], encoding='utf-8')

market_results = {}
duplicates = []
for candidate in data['candidates']:
    aliases = set(candidate['domains'])
    matches = [c for c in state['previouslyContacted'] if c.get('domain') in aliases or c.get('organisation', '').casefold() == candidate['organisation'].casefold()]
    assert not matches, 'New candidate already contacted'
    candidate['localLedgerDuplicateCheck'] = {'complete': True, 'manifestCount': len(manifests), 'priorConfirmedIds': 55, 'matches': []}
    regional = 'us' if candidate['market'] == 'United States' else 'middle-east'
    research_dir = root / 'docs/market' / regional / 'research/2026-10-11/082039Z'
    write(research_dir / 'qualification.json', candidate)
    notes = ['# ' + candidate['organisation'] + ' - held research', '', 'Checked: ' + candidate['checkedAt'], 'Offer: website/CMS. Buyer-confirmed need, budget and buying intent remain unknown.', '', candidate['observedFact'], '', 'Research quality: ' + ('Official showroom announcement inspected; published relevant email not verified.' if regional == 'us' else 'Official search excerpts only; full contact/article fetch unavailable. September opening chronology and .ae/.co.uk mail routing remain unresolved.'), '', 'Sources:']
    for evidence in candidate['needEvidence'] + candidate.get('dateConflictEvidence', []):
        notes.append('- ' + evidence['url'] + ' | source date: ' + str(evidence.get('sourceDate')) + ' | ' + evidence['observation'])
    notes += ['', 'Holds:'] + ['- ' + h for h in candidate['holdReasons']]
    notes += ['', 'No new official-source legal check completed; operational country rules were read. Existing CRM remains informational for this CMS offer. No no-CRM claim, supplier vacancy, dissatisfaction, transactional replacement or buying intent inferred.', '', 'Next action due: 2026-10-18, or earlier on new evidence. No forms, social messages, calls or permission-request marketing.']
    (research_dir / 'sources.md').write_text('\n'.join(notes) + '\n', encoding='utf-8')
    qualifier_path = root / 'docs/market' / regional / 'research/CRM-QUALIFICATION.json'
    qualifier = read(qualifier_path)
    assert not any(c.get('domain') == candidate['domain'] for c in qualifier.get('records', [])), 'Qualification already exists'
    qualifier.setdefault('records', []).append(candidate)
    qualifier.setdefault('qualificationRuns', []).append({'runId': data['runId'], 'at': stamp, 'candidate': candidate['organisation'], 'offerType': 'website_cms', 'eligible': False, 'reason': candidate['outreachDecision'], 'fullContactFetchComplete': False})
    qualifier['updatedAt'] = stamp
    write(qualifier_path, qualifier)
    assert not any(c.get('domain') == candidate['domain'] for c in pipeline['opportunities']), 'Pipeline already contains candidate'
    pipeline['opportunities'].append(candidate)
    market_results[candidate['market']] = {'status': 'research_held', 'freshResearch': candidate['organisation'], 'holdReasons': candidate['holdReasons'], 'qualification': rel(research_dir / 'qualification.json'), 'attemptedRecipient': None, 'messageIds': []}

pipeline['lastRunAt'] = stamp
write(pipeline_path, pipeline)
record = {'runId': data['runId'], 'startedAt': data['startedAt'], 'completedAt': stamp, 'localDate': '2026-10-11', 'status': 'completion_checks_failed', 'manifest': None, 'drafts': rel(run_dir / 'held-drafts.json'), 'markets': market_results, 'sentCount': 0, 'sendToolCalls': 0, 'confirmedMessageIds': [], 'verification': {'initialReplyComplete': True, 'initialReplyAt': data['evidence']['initial']['completedAt'], 'completionReplyComplete': False, 'initialExternalIdsVerified': 55, 'initialSentCount': 56, 'freshCandidateLiveDuplicatesComplete': False}, 'report': rel(run_dir / 'run-report.json')}
state.setdefault('scheduledRuns', []).append(record)
state['lastReplyCheckAt'] = data['evidence']['completion']['completedAt']
state['lastReplyCheckComplete'] = False
state['lastSuccessfulReplyCheckAt'] = data['evidence']['initial']['completedAt']
state['lastReplyCheckFailure'] = {'runId': data['runId'], 'at': data['evidence']['completion']['completedAt'], 'type': 'transport_failure', 'attempts': 3, 'error': data['evidence']['completion']['results'][0]['error'], 'nextAction': 'Restore/verify Gmail connectivity before another send run. Sending also remains blocked by session approval policy; do not bypass it.'}
state['senderBlock']['lastPolicyObservedAt'] = stamp
assert original_contacts == json.dumps(state['previouslyContacted'], sort_keys=True)
write(state_path, state)

prior_report = read(root / 'docs/market/acquisition/runs/2026-10-11/032405Z/run-report.json')
report = {**record, 'automationId': 'nexora-daily-us-sales-outreach', 'localStartedAt': '2026-10-11T09:20:39.795+01:00', 'localCompletedAt': local.isoformat(), 'timezone': 'Europe/London', 'confirmedNewSends': 0, 'replyChecks': {'initialComplete': True, 'initialCompletedAt': data['evidence']['initial']['completedAt'], 'associatedConversationsRead': 55, 'actualMessagesRead': [m['id'] for m in data['evidence']['initial']['actualMessages']], 'initialPositiveCount': 0, 'initialBounces': 0, 'completionComplete': False, 'completionOutcome': 'unknown_after_initial_success_due_transport_failure', 'completionFailureAttempts': 3, 'newCandidateDuplicateChecksComplete': False}, 'localGuards': {'manifestCount': len(manifests), 'pendingSendingUnknownOutcomes': 0, 'activeSendingRun': None, 'senderLockAbsent': True, 'rollingFiveHourConfirmedSends': len(recent), 'confirmedToday': len(today), 'perMarketCap': 1, 'totalCap': 2, 'dailyCap': 24}, 'attachment': attachment, 'senderBlock': {'reason': state['senderBlock']['reason'], 'alreadyNotified': True, 'sendCallRetried': False, 'whyNotRetried': 'Known session approval restriction; no approval configuration change. A successful read does not establish send permission.'}, 'webResearch': data['evidence']['researchFetch'], 'weeklyReview': {'performed': False, 'lastDate': pipeline['lastWeeklyCohortReviewDate'], 'nextDue': pipeline['nextWeeklyCohortReviewDate']}, 'proofDeliverables': prior_report['proofDeliverables'], 'partnerResearch': {'status': 'Existing research-only partner queue preserved; no actual founder-provided relationship or willingness established; no introduction messages sent'}, 'discardedResearch': [{'organisation': 'Nishi Enterprise', 'reason': 'Grand-opening URL redirected to homepage; current official event fact/date not verified, no pitch prepared'}], 'notification': 'Recurring completion-check transport failure and sender approval hold unchanged; no new substantive reply was observed in the completed initial check.', 'nextAction': 'Restore Gmail connectivity and allow Gmail send approval in the authorised session; repeat complete reply, suppression, outcome, duplicate and cap checks before retrying G&S or selecting another eligible prospect.', 'changes': 'Market research, local held drafts, shared state and pipeline only. Prior messages, suppression scopes, weekly evidence, application code and sibling projects preserved.'}
write(run_dir / 'run-report.json', report)
(run_dir / 'run-report.md').write_text('# Nexora five-hour dual-market run\n\n11 October 2026, ' + local.strftime('%H:%M') + ' Europe/London. No emails sent; zero send calls.\n\nThe completed initial reply/Sent check reconciled 55 external pitches and one self-test, read all 55 associated conversations and the three actual inbound messages. No new positive reply or bounce was observed then. The final reply check failed after three bounded transport attempts, so current reply status remains incomplete. The already reported send-approval block was not retried.\n\nUS: Atlas Restaurant Equipment showroom/CMS research held for published contact verification, final Gmail checks and sender permission. UAE: Honeypot Furniture content/CMS research held for full-page/contact routing verification, recipient-specific lawful route and Gmail checks. Both local body drafts have no selected recipient or embedded attachment and cannot be sent as-is.\n\nAll confirmed sends and suppressions preserved. Weekly review next due 17 October. Dedicated CMS/portal demonstrations and truthful public product page remain outstanding. Existing partner queue preserved without external contact.\n', encoding='utf-8')
print(json.dumps({'runId': data['runId'], 'status': record['status'], 'confirmedExternalHistory': 55, 'newSends': 0, 'sendCalls': 0, 'pipelineCount': len(pipeline['opportunities']), 'completionReplyCheck': False, 'lastSuccessfulReplyCheck': state['lastSuccessfulReplyCheckAt'], 'attachmentVerified': True, 'manifestsPreserved': len(manifests), 'nextWeeklyReview': pipeline['nextWeeklyCohortReviewDate'], 'report': record['report']}))
