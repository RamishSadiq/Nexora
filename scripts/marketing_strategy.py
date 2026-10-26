"""Offline Nexora campaign strategy audit and quality gates. Never sends email."""
import argparse
from collections import Counter, defaultdict
from datetime import datetime, timedelta, timezone
import json
from pathlib import Path
from zoneinfo import ZoneInfo, ZoneInfoNotFoundError

POLICY = Path('docs/market/acquisition/STRATEGY.json')
try:
    LONDON = ZoneInfo('Europe/London')
except ZoneInfoNotFoundError:
    # System Python on Windows may not include timezone data; use the existing
    # bundled IANA file without installing packages or changing global settings.
    _zone = Path.home()/'.cache/codex-runtimes/codex-primary-runtime/dependencies/python/Lib/site-packages/tzdata/zoneinfo/Europe/London'
    with _zone.open('rb') as _stream:
        LONDON = ZoneInfo.from_file(_stream, key='Europe/London')

def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))

def instant(value):
    dt = datetime.fromisoformat(value.replace('Z', '+00:00'))
    if dt.tzinfo is None:
        raise ValueError('Evidence timestamps must include a timezone')
    return dt.astimezone(timezone.utc)

def fresh(value, now, days):
    try:
        age = now-instant(value)
        return timedelta(0) <= age <= timedelta(days=days)
    except (TypeError, ValueError, AttributeError):
        return False

def full_weekdays(sent_at, now):
    date = instant(sent_at).astimezone(LONDON).date()+timedelta(days=1)
    end = now.astimezone(LONDON).date()
    count = 0
    while date < end:
        count += date.weekday() < 5
        date += timedelta(days=1)
    return count

def assess_candidate(candidate, policy, now=None, cohort_history=()):
    now = now or datetime.now(timezone.utc)
    reasons = []
    delivery = policy['deliveryReadiness']
    if delivery.get('status') != 'cleared':
        reasons.append('delivery_readiness_hold')
    review = delivery.get('incidentReview') or {}
    if review.get('decision') != 'cleared_with_limits' or not review.get('evidencePath') or not fresh(review.get('checkedAt'), now, policy['seedValidityDays']):
        reasons.append('documented_current_delivery_incident_review_required')
    valid_seeds = [s for s in delivery.get('seedChecks', []) if s.get('placement')=='inbox'
        and s.get('campaignEquivalent') is True and s.get('sender')==policy['sender']
        and s.get('messageId') and s.get('authenticationPassed') is True
        and fresh(s.get('checkedAt'), now, policy['seedValidityDays'])]
    if len({s.get('provider') for s in valid_seeds if s.get('provider')}) < policy['minimumDistinctSeedProviders']:
        reasons.append('current_campaign_equivalent_inbox_seed_checks_required')
    if candidate.get('market') not in policy['activeMarkets']:
        reasons.append('market_outside_authorised_scope')
    evidence = candidate.get('strategicEvidence') or {}
    cohort_id = evidence.get('cohortId')
    cohort = next((c for c in policy['cohorts'] if c['id']==cohort_id), None)
    if not cohort or cohort.get('status')!='active':
        reasons.append('active_focused_cohort_required')
    elif candidate.get('offerType')!=cohort['offerType']:
        reasons.append('offer_does_not_match_cohort')
    if cohort:
        sent = {m['messageId'] for m in cohort_history if m.get('messageId')
            and (m.get('strategicEvidence') or {}).get('cohortId')==cohort_id
            and (m.get('strategicEvidence') or {}).get('copyVersion')==cohort['copyVersion']}
        if len(sent)>=cohort['learningBatchMaximum']:
            reasons.append('cohort_learning_batch_full_review_before_more_sends')
    signal = evidence.get('signal') or {}
    if signal.get('kind') not in policy['acceptedSignalKinds']:
        reasons.append('workflow_or_website_existence_is_not_a_change_or_need_signal')
    if not all(signal.get(k) for k in ['url','detail','workflowRelevance']):
        reasons.append('signal_source_and_exact_workflow_relevance_required')
    if not fresh(signal.get('checkedAt'),now,policy['sourceValidityDays']):
        reasons.append('current_signal_check_required')
    if signal.get('kind')=='documented_current_manual_workflow':
        if signal.get('currentOfficialPublication') is not True:
            reasons.append('manual_workflow_must_be_current_official_evidence')
    else:
        try:
            age = now.astimezone(LONDON).date()-datetime.fromisoformat(signal.get('date','')).date()
            if age.days<0 or age.days>policy['maximumChangeSignalAgeDays']:
                reasons.append('change_signal_is_future_or_stale')
        except (TypeError, ValueError):
            reasons.append('dated_change_or_requirement_required')
    buyer = evidence.get('buyer') or {}
    if not all(buyer.get(k) for k in ['role','url','contactPurpose','offerRelevance']):
        reasons.append('responsible_function_and_contact_evidence_required')
    if buyer.get('publishedRoleRelevant') is not True:
        reasons.append('general_sales_or_enquiries_inbox_is_not_buyer_evidence')
    if not fresh(buyer.get('checkedAt'),now,policy['sourceValidityDays']):
        reasons.append('current_buyer_evidence_required')
    proof_id = (evidence.get('proof') or {}).get('assetId')
    asset = policy['proofAssets'].get(proof_id,{})
    if asset.get('status')!='verified' or candidate.get('offerType') not in asset.get('offers',[]):
        reasons.append('verified_offer_matched_proof_required')
    if cohort and evidence.get('copyVersion')!=cohort['copyVersion']:
        reasons.append('stable_registered_copy_version_required')
    if cohort and evidence.get('ctaVariant') not in cohort['allowedCtaVariants']:
        reasons.append('registered_cta_variant_required')
    if not evidence.get('offerHypothesis'):
        reasons.append('testable_offer_hypothesis_required')
    if candidate.get('offerType') in ['membership','company_contact_crm'] and candidate.get('crmStatus') not in ['confirmed_no_crm','manual_records_evidence']:
        reasons.append('existing_first_system_qualification_still_required')
    return {'version':policy['version'],'checkedAt':now.isoformat(),'strategyEligible':not reasons,'reasons':reasons,
        'notSendAuthorisation':True,'stillRequired':['live profiles and both-mailbox complete replies/outcomes',
        'country/contact rules and all suppressions','global duplicates, lock and rolling/daily caps',
        'current signature, attachment and Sent verification']}

def sent_history(root):
    result=[]
    for path in (root/'docs/market').rglob('campaign.json'):
        if 'cap-check-fixtures' not in path.parts:
            result.extend(m for m in read(path).get('messages',[]) if m.get('status')=='sent')
    return result

def assert_strategy_allows(candidate,root):
    result=assess_candidate(candidate,read(root/POLICY),cohort_history=sent_history(root))
    if not result['strategyEligible']:
        raise RuntimeError('Acquisition strategy hold: '+'; '.join(result['reasons']))

def audit(root,now=None):
    now=now or datetime.now(timezone.utc)
    state=read(root/'docs/market/us/daily/STATE.json')
    pipeline=read(root/'docs/market/acquisition/PIPELINE.json')
    opportunities={o['messageId']:o for o in pipeline['opportunities'] if o.get('messageId')}
    contacts={c['messageId']:c for c in state['previouslyContacted']}
    rows={};duplicate_ids=[]
    dimensions=['market','segment','offerType','acquisitionChannel','pitchVersion']
    for path in sorted((root/'docs/market').rglob('campaign.json')):
        if 'cap-check-fixtures' in path.parts:
            continue
        for message in read(path).get('messages',[]):
            mid=message.get('messageId')
            if message.get('status')!='sent' or not mid:
                continue
            if mid in rows:
                duplicate_ids.append(mid)
                continue
            merged={**contacts.get(mid,{}),**opportunities.get(mid,{}),**message}
            for key in dimensions:
                merged[key]=merged.get(key) or 'unattributed'
            merged['ledger']=path.relative_to(root).as_posix()
            try:
                merged['fullWeekdaysElapsed']=full_weekdays(merged['sentAt'],now)
            except (KeyError,ValueError,TypeError):
                merged['fullWeekdaysElapsed']=None
            rows[mid]=merged
    cohorts=defaultdict(list)
    for row in rows.values():
        cohorts[tuple(row[k] for k in dimensions)].append(row)
    positive_ids={r.get('originalMessageId') for r in state.get('positiveResponses',[]) if r.get('originalMessageId')}
    cohort_rows=[]
    def mature(m):
        return m['fullWeekdaysElapsed'] is not None and m['fullWeekdaysElapsed']>=7
    for keys,members in sorted(cohorts.items()):
        aged=[m for m in members if mature(m)]
        positive=sum(m['messageId'] in positive_ids for m in members)
        attributed=all(k!='unattributed' for k in keys)
        cohort_rows.append({**dict(zip(['market','segment','offerType','channel','copyVersion'],keys)),
            'uniqueSends':len(members),'matureSends':len(aged),'attributedPositiveReplies':positive,
            'fullyAttributed':attributed,'reviewRequired':attributed and len(aged)>=15 and positive==0,
            'missingAttributionReviewRequired':not attributed,
            'dates':[min(m.get('sentAt','') for m in members),max(m.get('sentAt','') for m in members)]})
    cms=[r for r in rows.values() if r['offerType']=='website_cms']
    queue=read(root/'docs/market/us/daily/ONE-TIME-FOLLOWUPS.json')
    followups={e['messageId'] for e in queue['entries'] if e.get('messageId') and e.get('status')=='sent_verified'}
    return {'generatedAt':now.isoformat(),'timezone':'Europe/London',
        'inboundVerifiedThrough':state.get('lastSuccessfulReplyCheckAt'),
        'uniqueFirstPitches':len(rows),'confirmedApprovedFollowups':len(followups-rows.keys()),
        'overallConfirmedMessages':len(rows.keys()|followups),
        'qualifiedPositiveResponsesRecorded':len(state.get('positiveResponses',[])),
        'deliveryAndInboxPlacement':'unknown_for_external_prospects',
        'callsHeld':None,'qualifiedOpportunities':None,'proposals':None,'acceptedPilots':None,'payingClients':None,
        'maturityRule':'7 complete Mon-Fri days; excludes send day and current partial day; public holidays not adjusted; operational proxy, not proof of failure',
        'matureFirstPitches':sum(mature(r) for r in rows.values()),
        'cms':{'sends':len(cms),'matureSends':sum(mature(r) for r in cms),
            'noBuyerConfirmedProblem':sum(not r.get('buyerConfirmedProblem') for r in cms),
            'scoredNeedOnlyWorkflow':sum((r.get('scores') or {}).get('need')==1 for r in cms),
            'unscoredNeed':sum('need' not in (r.get('scores') or {}) for r in cms),
            'unknownTiming':sum((r.get('scores') or {}).get('timing')==0 for r in cms),
            'countrySegmentOfferChannelCopyCohorts':sum(c['offerType']=='website_cms' for c in cohort_rows),
            'copyVersions':dict(Counter(r['pitchVersion'] for r in cms))},
        'unattributedOfferSends':sum(r['offerType']=='unattributed' for r in rows.values()),
        'dataQuality':{'duplicateLedgerMessageIds':duplicate_ids,
            'stateIdsMissingLedger':sorted(contacts.keys()-rows.keys()),
            'ledgerIdsMissingState':sorted(rows.keys()-contacts.keys()),
            'unattributedPositiveReplies':len(state.get('positiveResponses',[]))-len(positive_ids)},
        'cohorts':cohort_rows,
        'limitations':['Missing historical fields are not retroactively invented.',
            'No sending, inbox placement, opens or downstream outcomes inferred from acceptance.',
            'Signature edits are not content experiments; do not create a new copy version per day.']}

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode',choices=['audit','check'])
    parser.add_argument('--candidate',type=Path)
    parser.add_argument('--output',type=Path)
    parser.add_argument('--as-of')
    args=parser.parse_args()
    root=Path(__file__).resolve().parents[1]
    now=instant(args.as_of) if args.as_of else datetime.now(timezone.utc)
    if args.mode=='audit':
        result=audit(root,now)
    else:
        if not args.candidate:
            parser.error('--candidate is required for check')
        result=assess_candidate(read(args.candidate),read(root/POLICY),now,sent_history(root))
    if args.output:
        path=args.output.resolve();allowed=(root/'docs/market/acquisition').resolve()
        if not path.is_relative_to(allowed):
            parser.error('Output must be inside docs/market/acquisition')
        if path.name in ['PIPELINE.json','STRATEGY.json'] or 'runs' in path.relative_to(allowed).parts:
            parser.error('Use a distinct diagnostic/strategy-check output file')
        path.parent.mkdir(parents=True,exist_ok=True)
        path.write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
        print(json.dumps({'saved':str(path),'mode':args.mode,'strategyEligible':result.get('strategyEligible'),
            'uniqueFirstPitches':result.get('uniqueFirstPitches'),'cms':result.get('cms')}))
    else:
        print(json.dumps(result,indent=2))
    if args.mode=='check' and not result['strategyEligible']:
        raise SystemExit(2)

if __name__=='__main__':
    main()
