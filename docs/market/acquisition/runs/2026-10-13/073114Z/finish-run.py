import json, shutil, html
from pathlib import Path
from datetime import datetime, timezone, timedelta

root = Path(__file__).resolve().parents[6]
rd = Path(__file__).resolve().parent
rel = lambda path: str(path.relative_to(root)).replace('\\', '/')
def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))
def write(path, value):
    tmp = path.with_suffix(path.suffix + '.tmp')
    tmp.write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    tmp.replace(path)

d = read(rd / 'run-input.json')
e = read(rd / 'outcome-evidence.json')
sp = root / 'docs/market/us/daily/STATE.json'
qp = root / 'docs/market/us/daily/ONE-TIME-FOLLOWUPS.json'
pp = root / 'docs/market/acquisition/PIPELINE.json'
lock = root / 'docs/market/us/daily/SEND-LOCK.json'
s, q, p = read(sp), read(qp), read(pp)
assert s.get('activeSendingRun', {}).get('runId') == d['runId']
assert read(lock)['runId'] == d['runId']
assert s['campaignStatus'] == 'active'
assert e['completion']['complete'] and e['completion']['sentReconciliation']['complete']
assert not e['completion']['newPositiveReplies']
assert len(s['previouslyContacted']) == 59
assert all(c['messageId'] in e['completion']['sentReconciliation']['historicalIds'] for c in s['previouslyContacted'])
assert [entry['status'] for entry in q['entries']] == ['blocked_unsent', 'queued']
assert not any(entry.get('messageId') or entry.get('sentAt') for entry in q['entries'])
assert e['sendAttempt']['result']['isError']
reason = 'MCP tool call requires approval, but approval policy is never'
assert reason in str(e['sendAttempt']['result'])
assert q['entries'][0]['sentReconciliation']['exactRecipientSubjectMatches'] == 0
prior_contacts = json.dumps(s['previouslyContacted'], sort_keys=True)
prior_hourly = json.dumps(s.get('hourlyRuns'), sort_keys=True)
now = datetime.now(timezone.utc)
stamp = now.isoformat()
local = now.astimezone(timezone(timedelta(hours=1)))

followups = []
for entry in q['entries']:
    original = next(c for c in s['previouslyContacted'] if c['messageId'] == entry['originalPitchMessageId'])
    o = next((o for o in p['opportunities'] if o.get('domain') == original['domain']), None)
    if o is None:
        o = {**original, 'market': 'United States', 'country': 'United States',
             'offerType': 'membership', 'stage': 'sent', 'owner': 'Ramish Qureshi',
             'buyerConfirmedProblem': None, 'budget': None, 'decisionDate': None,
             'acquisitionChannel': 'individual_cold_email', 'history': []}
        p['opportunities'].append(o)
    o.setdefault('history', []).append({'at': stamp, 'runId': d['runId'],
        'event': 'Approved one-time saved draft retained unsent', 'followupEntryId': entry['id'],
        'draftId': entry['draftId'], 'recipient': entry['to'], 'status': entry['status'],
        'acceptedMessageId': None, 'reason': reason if entry['status'] == 'blocked_unsent' else 'Not attempted after current send denial'})
    o['oneTimeFollowupQueue'] = rel(qp)
    o['nextAction'] = 'Retain exact approved draft for next eligible scheduled run; reconcile replies, Sent and current send access first.'
    followups.append({'entryId': entry['id'], 'organisation': entry['organisation'],
        'to': entry['to'], 'draftId': entry['draftId'], 'status': entry['status'],
        'attempts': 1 if entry['status'] == 'blocked_unsent' else 0, 'messageIds': [],
        'sentReconciliationComplete': True})

for market in ['Australia', 'New Zealand', 'Canada']:
    c = e['research']['markets'][market]
    o = next((o for o in p['opportunities'] if o.get('domain') == c['domain']), None)
    if o is None:
        o = {'organisation': c['organisation'], 'domain': c['domain'], 'history': [],
             'buyerConfirmedProblem': None, 'budget': None, 'decisionDate': None,
             'currentProvider': 'unknown', 'owner': 'Ramish Qureshi'}
        p['opportunities'].append(o)
    # Existing source and buyer history is retained; current assessment is appended.
    o.update({**c, 'market': market, 'country': market, 'sendEligible': False,
        'acquisitionChannel': 'individual_cold_email', 'pitchVersion': 'website-cms-v2',
        'proofAsset': 'docs/market/pitch/nexora-product-demo-male.mp4',
        'proofGap': 'Existing video covers wider membership platform; dedicated CMS demonstration outstanding',
        'stage': 'held_sender_approval' if c['qualificationEligible'] else 'research_only',
        'capabilityFit': 'Staff-managed branded public information pages with draft, preview and publication',
        'unsupportedRequirements': ['External CRM integration', 'Automatic migration', 'Arbitrary workflows', 'Booking or ecommerce'],
        'countryRulesCheckedAt': c['sources'][0]['checkedAt']})
    o.setdefault('history', []).append({'at': stamp, 'runId': d['runId'],
        'event': 'Current official-source assessment saved; no pitch sent',
        'observedFact': c['observedFact'], 'qualificationEligible': c['qualificationEligible'],
        'sources': c['sources'], 'holdReasons': c['holdReasons']})
gs = next((o for o in p['opportunities'] if o.get('domain') == 'gsrestaurantequipment.com'), None)
if gs:
    gs.setdefault('history', []).append({'at': stamp, 'runId': d['runId'],
        'event': 'Additional US first pitch deferred under authorized one-time MPIF/SMI pair policy; no catch-up capacity'})
p['lastRunAt'] = stamp

for suffix in ['txt', 'html', 'eml']:
    old = root / ('docs/market/acquisition/runs/2026-10-13/023039Z/australia.' + suffix)
    if old.exists():
        shutil.copyfile(old, rd / ('australia-held.' + suffix))
body = """Hello Brian,

I saw Smith + Andersen's October 1 announcement about your new Victoria office.

I'm Ramish, founder of Nexora. We've built a modular platform with a website CMS that lets staff draft, preview and publish branded public pages. A small evaluation could cover three office, service and news pages with one staff editor.

The attached 90-second narrated and captioned video introduces Nexora's wider membership platform. A CMS walkthrough would focus on editing and publishing your public content.

Would a 15-minute editor walkthrough be useful for your office and service information?

Best,
Ramish Qureshi
Founder, Nexora
ramishqureshi99@gmail.com

Advertisement: Nexora website/CMS software.
EOBI Building, I-8 Markaz, Islamabad 44790, Pakistan.
To stop all marketing emails from Nexora, reply 'unsubscribe' or 'no thanks' and we won't contact you again.
"""
subject = 'Staff-managed office and service content for Smith + Andersen'
(rd / 'canada-held.txt').write_text('HELD LOCAL PREPARATION - NOT SENT\nSubject: ' + subject + '\nTo: Brian.Brett@smithandandersen.com\n\n' + body, encoding='utf-8')
(rd / 'canada-held.html').write_text('<!doctype html><html><body>' + ''.join('<p>' + html.escape(x).replace('\n', '<br>') + '</p>' for x in body.strip().split('\n\n')) + '</body></html>', encoding='utf-8')
write(rd / 'held-draft-metadata.json', {'status': 'held_not_sent', 'newGmailDraftsCreated': 0,
    'Canada': {'to': 'Brian.Brett@smithandandersen.com', 'subject': subject,
               'requiredAttachment': e['research']['attachment'], 'mimeNotBuilt': True},
    'Australia': {'reusedPreparation': 'australia-held.eml', 'oldestEligibleUnsent': True},
    'New Zealand': {'draftNotAddressed': True, 'reason': 'Recipient country and contact route unresolved'}})

old = s.get('senderBlock', {})
s.setdefault('senderBlockHistory', []).append({'at': stamp, 'event': 'Current authorized saved-draft send access tested', 'previous': old})
s['senderBlock'] = {**old, 'active': True, 'at': e['sendAttempt']['at'], 'reason': reason,
    'scope': 'current scheduled session saved-draft send action', 'confirmedUnsentRecipient': 'sgibbs@mpif.org',
    'evidence': rel(rd / 'outcome-evidence.json'), 'sendAttempts': 1, 'notified': True,
    'action': 'Scheduled execution must permit authorized Gmail send actions. Current approval policy never blocked the draft despite standing user authority; do not bypass restrictions or change sender.'}
s.setdefault('sendFailures', []).append({'runId': d['runId'], 'at': e['sendAttempt']['at'],
    'market': 'United States', 'recipient': 'sgibbs@mpif.org', 'messageType': 'one_time_followup',
    'draftId': q['entries'][0]['draftId'], 'reason': reason, 'attempts': 1,
    'acceptedMessageId': None, 'sentReconciliationComplete': True, 'confirmedAbsentInSent': True})
run = next(r for r in s['scheduledRuns'] if r['runId'] == d['runId'])
run.update({'completedAt': stamp, 'status': 'draft_send_permission_denied_confirmed_unsent',
    'followups': followups, 'sentCount': 0, 'firstPitchSentCount': 0, 'followupSentCount': 0,
    'sendToolCalls': 1, 'confirmedMessageIds': [],
    'markets': {'United States': {'status': 'approved_pair_pending_no_additional_first_pitch'},
        'Australia': {'status': 'oldest_eligible_prepared_current_send_denial', 'organisation': 'JAE Engineering Group'},
        'Canada': {'status': 'prepared_current_send_denial', 'organisation': 'Smith + Andersen'},
        'New Zealand': {'status': 'held_recipient_country_and_contact_route', 'organisation': 'Lockton New Zealand'}},
    'verification': {'initialReplyComplete': True, 'completionReplyComplete': True,
        'historicalExternalMessagesVerified': 59, 'sentFilenameMatchesIncludingSelfTest': 60,
        'keywordSearchIndexDiscrepancy': '52 keyword matches; complete filename search contains all historical IDs',
        'newPositiveReplies': 0, 'newOptOuts': 0, 'newBounces': 0,
        'followupAlreadySentMatches': 0, 'approvedDraftsRemainPresent': True}})
s.update({'lastReplyCheckAt': e['completion']['at'], 'lastSuccessfulReplyCheckAt': e['completion']['at'],
    'lastReplyCheckComplete': True, 'lastReplyCheckFailure': None,
    'lastReplyCompletionEvidence': rel(rd / 'completion-checks.json'),
    'lastCampaignRunAt': stamp, 'activeSendingRun': None})
q['lastUpdatedAt'] = stamp
q['lastCompletedRunId'] = d['runId']
q['lastCompletionCheckAt'] = e['completion']['at']
q['pendingEntryIds'] = [x['id'] for x in q['entries'] if x['status'] not in ['sent_verified', 'suppressed']]
assert prior_contacts == json.dumps(s['previouslyContacted'], sort_keys=True)
assert prior_hourly == json.dumps(s.get('hourlyRuns'), sort_keys=True)
write(rd / 'completion-checks.json', e['completion'])
report = {**run, 'automationId': q['automationId'], 'timezone': 'Europe/London',
    'localStartedAt': '2026-10-13T08:31:14.036+01:00', 'localCompletedAt': local.isoformat(),
    'pendingQueue': rel(qp), 'actualSendFailure': e['sendAttempt'], 'research': e['research'],
    'capsTests': read(rd / 'cap-check-results.json'), 'weeklyReview': {'performed': False, 'nextDue': '2026-10-17'},
    'proofDeliverables': ['Dedicated CMS demonstration outstanding', 'Dedicated portal demonstration outstanding', 'Truthful public product page outstanding'],
    'restrictions': 'No sender/channel bypass, follow-up expansion, auto replies, meetings, forms, deployments, application edits, commits or pushes',
    'partnerResearch': {'queuePreserved': True, 'messagesSent': 0}, 'notificationRequired': True}
write(rd / 'run-report.json', report)
(rd / 'run-report.md').write_text('# Scheduled run: 13 October 2026\n\n08:31 Europe/London start. Zero emails sent. The exact approved MPIF draft was denied before acceptance: `MCP tool call requires approval, but approval policy is never`. Sent confirms no Stephanie follow-up; Gary remains queued and was not attempted. Both original saved drafts remain present and unchanged.\n\nGlobal overlapping inbound, delivery, spam/trash, prior-domain and conversation checks completed before the attempt and at completion. No new human interest, refusal or bounce. All 59 historical external first-pitch IDs remain in Sent. The keyword search returned only 52, so the complete 60-message attachment search was used to reconcile every historical ID.\n\nThe pair reserves two overall slots; no additional US first pitch was attempted. JAE is the oldest eligible Australian preparation; Smith + Andersen has a newly verified relevant published marketing contact and Victoria-office signal. Both preparations are held by current send access. Lockton NZ is held because the Pacific marketing contact country and applicable route remain unresolved. No unused slot transfers or catch-up.\n\nThe durable follow-up queue now participates in shared rolling/daily message guards. Five meaningful cap/outcome fixture checks passed. Existing first-pitch timestamps and unique-organisation counts are preserved. Weekly review is due October 17. CMS/portal demonstrations and public product page remain outstanding.\n', encoding='utf-8')
notes = ['# Current official sources and decisions', '', 'Checked ' + e['research']['at'], '']
for market, c in e['research']['markets'].items():
    notes += ['## ' + market + ': ' + c['organisation'], c['observedFact'],
              'Hold: ' + '; '.join(c['holdReasons']), '']
    for source in c.get('sources', []):
        notes.append('- ' + source['url'] + ' | source date ' + str(source.get('sourceDate')) + ' | checked ' + source['checkedAt'])
    notes += ['Contact assessment: ' + c.get('outreachLegalBasis', 'Approved one-time queue; user authority is distinct from recipient interest'), '']
(rd / 'sources.md').write_text('\n'.join(notes) + '\n', encoding='utf-8')
write(qp, q)
write(pp, p)
write(sp, s)
assert read(lock)['runId'] == d['runId']
lock.unlink()
print(json.dumps({'runId': d['runId'], 'sent': 0, 'currentActualSendDenied': True,
    'pendingApprovedFollowups': len(q['pendingEntryIds']), 'historyPreserved': 59,
    'globalCompletionChecksComplete': True, 'ownLockReleased': not lock.exists(),
    'report': rel(rd / 'run-report.md')}))
