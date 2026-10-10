"""Persist US outreach state only; this script never sends email."""
import argparse
from datetime import datetime, timedelta, timezone
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--recipient', required=True)
parser.add_argument('--status', required=True, choices=['ready','sending','sent','held','failed','unknown'])
parser.add_argument('--message-id')
parser.add_argument('--thread-id')
parser.add_argument('--reason')
parser.add_argument('--campaign', default='docs/market/us/outreach/2026-10-08/campaign.json',
                    help='US daily manifest path, relative to the Nexora repository or absolute')
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent
requested = Path(args.campaign)
path = (requested if requested.is_absolute() else root / requested).resolve()
allowed = (root / 'docs/market/us/outreach').resolve()
if not path.is_relative_to(allowed) or path.name != 'campaign.json':
    raise ValueError('Campaign manifest must remain within Nexora docs/market/us/outreach')
campaign = json.loads(path.read_text(encoding='utf-8'))
message = next(m for m in campaign['messages'] if m['to'] == args.recipient)
daily_path = root / 'docs/market/us/daily/STATE.json'
daily_state = json.loads(daily_path.read_text(encoding='utf-8')) if campaign.get('dailyCampaign') else None
if daily_state and args.status == 'sending':
    if daily_state['campaignStatus'] != 'active':
        raise RuntimeError('Daily campaign is stopped; sending is not allowed')
    already_today = sum(c.get('localDate') == campaign['localDate'] for c in daily_state['previouslyContacted'])
    if already_today >= daily_state['dailyNewOrganisationLimit']:
        raise RuntimeError('Daily organisation limit reached')
    if daily_state.get('cadence') == 'hourly':
        current_hour = datetime.now(timezone.utc).replace(minute=0, second=0, microsecond=0)
        confirmed_this_hour = sum(
            datetime.fromisoformat(c['sentAt'].replace('Z', '+00:00')).astimezone(timezone.utc)
            .replace(minute=0, second=0, microsecond=0) == current_hour
            for c in daily_state['previouslyContacted'] if c.get('sentAt')
        )
        if confirmed_this_hour >= daily_state['hourlyNewOrganisationLimit']:
            raise RuntimeError('Hourly organisation limit reached')
    elif daily_state.get('cadence') == 'five_hourly':
        cutoff = datetime.now(timezone.utc) - timedelta(hours=daily_state['intervalHours'])
        recent = [c for c in daily_state['previouslyContacted'] if c.get('sentAt') and
                  datetime.fromisoformat(c['sentAt'].replace('Z', '+00:00')).astimezone(timezone.utc) > cutoff]
        market = message.get('market')
        if market not in daily_state['activeMarkets']:
            raise RuntimeError('A current market label is required')
        if len(recent) >= daily_state['newOrganisationLimitPerInterval']:
            raise RuntimeError('Five-hour total organisation limit reached')
        if sum(c.get('market') == market for c in recent) >= daily_state['newOrganisationLimitPerMarketPerInterval']:
            raise RuntimeError('Five-hour market organisation limit reached')
if message['status'] == 'sent' and args.status != 'sent':
    raise RuntimeError('Never reset a confirmed send')
if message['status'] == 'sent' and args.message_id and message.get('messageId') != args.message_id:
    raise RuntimeError('Never replace a confirmed Gmail message ID')
if args.status == 'sent' and not args.message_id:
    raise RuntimeError('Confirmed Gmail message ID required')
now = datetime.now(timezone.utc).isoformat()
message['status'] = args.status
message['updatedAt'] = now
if args.message_id:
    message['messageId'] = args.message_id
    message['threadId'] = args.thread_id
    message['sentAt'] = message.get('sentAt', now)
if args.reason:
    message['statusReason'] = args.reason
message.setdefault('events', []).append({'at':now, 'status':args.status, 'reason':args.reason, 'messageId':args.message_id})
temp = path.with_suffix('.json.tmp')
temp.write_text(json.dumps(campaign, indent=2, ensure_ascii=False), encoding='utf-8')
temp.replace(path)
if daily_state and args.status == 'sent':
    contact = {'organisation':message['organisation'], 'email':message['to'],
               'domain':message['to'].split('@')[1].lower(), 'messageId':args.message_id,
               'threadId':args.thread_id, 'sentAt':message['sentAt'], 'localDate':campaign['localDate'],
               'market':message.get('market'), 'country':message.get('country'), 'offerType':message.get('offerType')}
    if not any(c['messageId'] == args.message_id for c in daily_state['previouslyContacted']):
        daily_state['previouslyContacted'].append(contact)
    if daily_state.get('cadence') == 'hourly':
        utc_hour = datetime.now(timezone.utc).replace(minute=0, second=0, microsecond=0).isoformat()
        hourly_runs = daily_state.setdefault('hourlyRuns', [])
        run = next((r for r in hourly_runs if r['utcHour'] == utc_hour), None)
        if run is None:
            run = {'date':campaign['localDate'], 'utcHour':utc_hour, 'startedAt':now,
                   'manifest':str(path.relative_to(root)), 'messageIds':[]}
            hourly_runs.append(run)
        if args.message_id not in run['messageIds']:
            run['messageIds'].append(args.message_id)
        run['status'] = 'sent_pending_verification'
        run['sentCount'] = len(run['messageIds'])
        run['updatedAt'] = now
    else:
        run = next((r for r in daily_state['runs'] if r['date'] == campaign['localDate']), None)
        if run is None:
            run = {'date':campaign['localDate'], 'startedAt':now, 'manualStart':campaign.get('manualStart',False)}
            daily_state['runs'].append(run)
        run['status'] = 'sending'
        run['messageIds'] = [m['messageId'] for m in campaign['messages'] if m['status'] == 'sent']
        run['sentCount'] = len(run['messageIds'])
        run['updatedAt'] = now
    state_temp = daily_path.with_suffix('.json.tmp')
    state_temp.write_text(json.dumps(daily_state,indent=2,ensure_ascii=False),encoding='utf-8')
    state_temp.replace(daily_path)
print(json.dumps({'to':message['to'], 'status':message['status'], 'messageId':message.get('messageId'), 'sent':sum(m['status']=='sent' for m in campaign['messages'])}))
