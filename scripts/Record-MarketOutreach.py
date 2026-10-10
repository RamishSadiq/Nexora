"""Persist a confirmed Gmail send or held prospect; does not send mail."""
import argparse
from datetime import datetime, timezone
import json
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('--recipient', required=True)
p.add_argument('--status', required=True, choices=['sent', 'held', 'ready', 'sending', 'failed', 'unknown'])
p.add_argument('--message-id')
p.add_argument('--thread-id')
p.add_argument('--reason')
p.add_argument('--company-number')
a = p.parse_args()
path = Path(__file__).resolve().parent.parent / 'docs/market/outreach/2026-10-08/campaign.json'
campaign = json.loads(path.read_text(encoding='utf-8'))
item = next(m for m in campaign['messages'] if m['to'] == a.recipient)
if item['status'] == 'sent' and a.status != 'sent':
    raise RuntimeError('Preserve the confirmed send; do not reset its status')
if a.status == 'sent' and not a.message_id:
    raise RuntimeError('A confirmed message ID is required')
item['status'] = a.status
item['updatedAt'] = datetime.now(timezone.utc).isoformat()
if a.message_id:
    item['messageId'] = a.message_id
    item['threadId'] = a.thread_id
    item['sentAt'] = item['updatedAt']
if a.reason:
    item['holdReason' if a.status == 'held' else 'statusReason'] = a.reason
if a.company_number:
    item['companyNumber'] = a.company_number
    item['companySource'] = 'https://find-and-update.company-information.service.gov.uk/company/' + a.company_number
temp = path.with_suffix('.json.tmp')
temp.write_text(json.dumps(campaign, indent=2), encoding='utf-8')
temp.replace(path)
print(json.dumps({'to': item['to'], 'status': item['status'], 'messageId': item.get('messageId')}))
