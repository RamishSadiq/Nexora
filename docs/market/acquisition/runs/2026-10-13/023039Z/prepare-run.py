import json, hashlib
from pathlib import Path
from datetime import datetime, timezone, timedelta
from email.message import EmailMessage
from email import policy

root=Path(__file__).resolve().parents[6]; rd=Path(__file__).resolve().parent
rel=lambda p:str(p.relative_to(root)).replace('\\','/')
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):
 p.parent.mkdir(parents=True,exist_ok=True)
 t=p.with_suffix(p.suffix+'.tmp');t.write_text(json.dumps(d,indent=2,ensure_ascii=False)+'\n',encoding='utf-8');t.replace(p)
d=read(rd/'run-input.json');sp=root/'docs/market/us/daily/STATE.json';pp=root/'docs/market/acquisition/PIPELINE.json'
s=read(sp);p=read(pp);now=datetime.now(timezone.utc);stamp=now.isoformat()
assert s['campaignStatus']=='active' and len(s['previouslyContacted'])==59
assert s['activeMarkets']==['Australia','United States','New Zealand','Canada']
assert not s.get('activeSendingRun') and not (root/'docs/market/us/daily/SEND-LOCK.json').exists()
assert not any(r.get('runId')==d['runId'] for r in s.get('scheduledRuns',[]))
# BST applies to this dated run; do not reuse this offset across DST boundaries.
assert now.astimezone(timezone(timedelta(hours=1))).date().isoformat()==d['date']
recent=[c for c in s['previouslyContacted'] if c.get('sentAt') and datetime.fromisoformat(c['sentAt'].replace('Z','+00:00'))>now-timedelta(hours=5)]
today=[c for c in s['previouslyContacted'] if c.get('localDate')==d['date']]
assert len(recent)==0 and len(today)==0
ids=[];pending=[];ledgers=list((root/'docs/market').rglob('campaign.json'))
for path in ledgers:
 for m in read(path).get('messages',[]):
  if m.get('status')=='sent' and m.get('messageId'):ids.append(m['messageId'])
  if m.get('status') in ['sending','unknown','send_unknown']:pending.append(m)
assert len(set(ids))==59 and not pending
sent=set(d['evidence']['initial']['searches'][-2]['ids'])
assert all(c['messageId'] in sent for c in s['previouslyContacted'])
assert all(not x['emails'] and x['complete'] for x in d['evidence']['candidateDuplicates'])
sup=read(root/'docs/market/OUTREACH-SUPPRESSIONS.json')
vb=(root/'docs/market/pitch/nexora-product-demo-male.mp4').read_bytes();vh=hashlib.sha256(vb).hexdigest().upper()
assert len(vb)==s['attachment']['size_bytes'] and vh==s['attachment']['sha256'].upper()
cp=root/'docs/market/us/outreach'/d['date']/'campaign.json'
campaign=read(cp) if cp.exists() else {'campaign':'Nexora four-country first pitches','createdAt':stamp,'localDate':d['date'],'dailyCampaign':True,'sender':s['sender'],'postalAddress':s['postalAddress'],'attachment':s['attachment'],'messages':[],'runIds':[]}
assert campaign['dailyCampaign'] and campaign['localDate']==d['date']
assert not any(m.get('to')==c.get('buyerContact') for m in campaign['messages'] for c in d['candidates'] if c['sendEligible'])
write(rd/'reply-and-sent-checks.json',d['evidence']['initial']);write(rd/'candidate-duplicate-checks.json',d['evidence']['candidateDuplicates'])
verification=[]
for draft in d['drafts']:
 draft['body']=draft['body'].replace('three sample showroom information, service pages and company news','three sample pages covering showroom information, services and company news')
 if draft['market']=='United States':
  import html
  draft['html']='<div style="font-family:Arial,sans-serif;font-size:14px;line-height:1.5">'+''.join('<p>'+html.escape(x).replace('\n','<br>')+'</p>'for x in draft['body'].split('\n\n'))+'</div>'
 slug=draft['market'].lower().replace(' ','-')
 (rd/(slug+'.txt')).write_text('Subject: '+draft['subject']+'\nTo: '+str(draft['to'])+'\n\n'+draft['body']+'\n',encoding='utf-8')
 (rd/(slug+'.html')).write_text(draft['html'],encoding='utf-8')
 if draft['to']:
  em=EmailMessage(policy=policy.SMTP)
  for k,v in [('From',draft['from']),('Reply-To',draft['replyTo']),('To',draft['to']),('Subject',draft['subject'])]:em[k]=v
  em.set_content(draft['body']);em.add_alternative(draft['html'],subtype='html');em.add_attachment(vb,maintype='video',subtype='mp4',filename='nexora-product-demo-male.mp4')
  f=rd/(slug+'.eml');f.write_bytes(em.as_bytes());draft['mimeFile']=rel(f)
  verification.append({'market':draft['market'],'recipient':draft['to'],'filename':'nexora-product-demo-male.mp4','mime':'video/mp4','bytes':len(vb),'sha256':vh,'prepared':True})
for c in d['candidates']:
 assert not any(x.get('domain') in c['domains'] or x['organisation'].casefold()==c['organisation'].casefold() for x in s['previouslyContacted'])
 assert not any(x.get('domain') in c['domains'] or (c.get('buyerContact') and x.get('email')==c['buyerContact']) for x in sup['entries'])
 old=next((x for x in p['opportunities'] if x.get('domain')==c['domain']),None)
 if old:
  history=old.get('history',[])+c['history'];old.update(c);old['history']=history
 else:p['opportunities'].append(c)
 if c['sendEligible']:
  draft=next(x for x in d['drafts'] if x['market']==c['market'])
  campaign['messages'].append({**c,'to':draft['to'],'subject':draft['subject'],'status':'ready','bodyText':draft['body'],'bodyHtml':draft['html'],'mimeFile':draft['mimeFile'],'events':[{'at':stamp,'status':'ready','reason':'Current official routes, complete global replies, duplicates and caps checked'}]})
write(rd/'candidates.json',d['candidates']);write(rd/'drafts.json',d['drafts']);write(rd/'draft-verification.json',verification)
write(rd/'pre-send-guards.json',{'at':stamp,'externalHistory':59,'manifestsChecked':len(ledgers),'pendingOutcomes':0,'recentFiveHourCount':0,'todayCount':0,'attachment':{'sizeBytes':len(vb),'sha256':vh,'matchesState':True},'positiveReplies':0,'replyComplete':True})
p['lastRunAt']=stamp;write(pp,p)
lock=root/'docs/market/us/daily/SEND-LOCK.json'
with lock.open('x',encoding='utf-8') as fh:json.dump({'runId':d['runId'],'startedAt':stamp,'runDirectory':rel(rd)},fh)
s['activeSendingRun']={'runId':d['runId'],'startedAt':stamp,'manifest':rel(cp),'lock':rel(lock)}
s.setdefault('scheduledRuns',[]).append({'runId':d['runId'],'startedAt':d['startedAt'],'localDate':d['date'],'status':'preparing_sends','markets':{c['market']:{'organisation':c['organisation'],'status':'ready' if c['sendEligible'] else 'held','holdReasons':c['holdReasons'],'messageIds':[]}for c in d['candidates']},'sentCount':0,'confirmedMessageIds':[],'manifest':rel(cp),'report':rel(rd/'run-report.json')})
s['lastReplyCheckAt']=d['evidence']['initial']['at'];s['lastReplyCheckComplete']=True;s['lastSuccessfulReplyCheckAt']=d['evidence']['initial']['at'];s['lastReplyCheckEvidence']=rel(rd/'reply-and-sent-checks.json')
campaign['runIds'].append(d['runId']);write(cp,campaign);write(sp,s)
print(json.dumps({'runId':d['runId'],'ready':[c['market']for c in d['candidates']if c['sendEligible']],'held':[c['market']for c in d['candidates']if not c['sendEligible']],'attachmentVerified':True,'lockCreated':True,'history':59,'rolling':0,'today':0}))
