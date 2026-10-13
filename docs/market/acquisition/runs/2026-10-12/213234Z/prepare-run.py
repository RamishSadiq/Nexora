import json,hashlib
from pathlib import Path
from datetime import datetime,timezone,timedelta
from email.message import EmailMessage
from email import policy

root=Path(__file__).resolve().parents[6]
rd=Path(__file__).resolve().parent
rel=lambda p:str(p.relative_to(root)).replace('\\','/')
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):
 p.parent.mkdir(parents=True,exist_ok=True)
 t=p.with_suffix(p.suffix+'.tmp');t.write_text(json.dumps(d,indent=2,ensure_ascii=False)+'\n',encoding='utf-8');t.replace(p)
d=read(rd/'run-input.json');sp=root/'docs/market/us/daily/STATE.json';pp=root/'docs/market/acquisition/PIPELINE.json'
s=read(sp);p=read(pp);cp=root/'docs/market/us/outreach'/d['date']/'campaign.json';campaign=read(cp)
now=datetime.now(timezone.utc);stamp=now.isoformat()
assert s['campaignStatus']=='active' and len(s['previouslyContacted'])==59
assert s['activeMarkets']==['Australia','United States','New Zealand','Canada']
assert not s.get('activeSendingRun') and not (root/'docs/market/us/daily/SEND-LOCK.json').exists()
assert not any(r.get('runId')==d['runId'] for r in s.get('scheduledRuns',[]))
assert now.astimezone(timezone(timedelta(hours=1))).date().isoformat()==d['date']
recent=[c for c in s['previouslyContacted'] if c.get('sentAt') and datetime.fromisoformat(c['sentAt'].replace('Z','+00:00'))>now-timedelta(hours=5)]
assert len(recent)==0
assert sum(c.get('localDate')==d['date'] for c in s['previouslyContacted'])==4
ids=[];pending=[];ledger_paths=list((root/'docs/market').rglob('campaign.json'))
for path in ledger_paths:
 for m in read(path).get('messages',[]):
  if m.get('status')=='sent' and m.get('messageId'):ids.append(m['messageId'])
  if m.get('status') in ['sending','unknown','send_unknown']:pending.append(m)
assert len(set(ids))==59 and not pending
sup=read(root/'docs/market/OUTREACH-SUPPRESSIONS.json')
sent=set(d['evidence']['initial']['searches'][-2]['ids'])
assert all(c['messageId'] in sent for c in s['previouslyContacted'])
assert all(not x['emails'] and x['complete'] for x in d['evidence']['candidateDuplicates'])
video=root/'docs/market/pitch/nexora-product-demo-male.mp4';vb=video.read_bytes();vh=hashlib.sha256(vb).hexdigest().upper()
assert len(vb)==s['attachment']['size_bytes'] and vh==s['attachment']['sha256'].upper()
write(rd/'reply-and-sent-checks.json',d['evidence']['initial']);write(rd/'candidate-duplicate-checks.json',d['evidence']['candidateDuplicates'])
verification=[]
for draft in d['drafts']:
 slug=draft['market'].lower().replace(' ','-')
 (rd/(slug+'.txt')).write_text('Subject: '+draft['subject']+'\nTo: '+str(draft['to'])+'\n\n'+draft['body']+'\n',encoding='utf-8')
 (rd/(slug+'.html')).write_text(draft['html'],encoding='utf-8')
 if draft['to']:
  em=EmailMessage(policy=policy.SMTP)
  for k,v in [('From',draft['from']),('Reply-To',draft['replyTo']),('To',draft['to']),('Subject',draft['subject'])]:em[k]=v
  em.set_content(draft['body']);em.add_alternative(draft['html'],subtype='html');em.add_attachment(vb,maintype='video',subtype='mp4',filename=video.name)
  file=rd/(slug+'.eml');file.write_bytes(em.as_bytes());draft['mimeFile']=rel(file)
  verification.append({'market':draft['market'],'recipient':draft['to'],'filename':video.name,'mime':'video/mp4','bytes':len(vb),'sha256':vh,'prepared':True})
for c in d['candidates']:
 assert not any(x.get('domain') in c['domains'] or x['organisation'].casefold()==c['organisation'].casefold() for x in s['previouslyContacted'])
 assert not any(x.get('domain') in c['domains'] or (c.get('buyerContact') and x.get('email')==c['buyerContact']) for x in sup['entries'])
 c['runId']=d['runId']
 old=next((x for x in p['opportunities'] if x.get('domain')==c['domain']),None)
 if old:
  assert c['organisation']=='G&S Restaurant Equipment'
  history=old.get('history',[])+c['history'];old.update(c);old['history']=history
 else:p['opportunities'].append(c)
 if c['sendEligible']:
  draft=next(x for x in d['drafts'] if x['market']==c['market'])
  assert not any(m.get('to','').lower()==draft['to'].lower() for m in campaign['messages'])
  campaign['messages'].append({**c,'to':draft['to'],'subject':draft['subject'],'status':'ready','bodyText':draft['body'],'bodyHtml':draft['html'],'mimeFile':draft['mimeFile'],'events':[{'at':stamp,'status':'ready','reason':'Official/source/route, global replies, duplicates and caps passed'}]})
write(rd/'candidates.json',d['candidates']);write(rd/'drafts.json',d['drafts']);write(rd/'draft-verification.json',verification)
write(rd/'pre-send-guards.json',{'at':stamp,'externalHistory':59,'manifests':len(ledger_paths),'pendingOutcomes':0,'recentFiveHourCount':0,'todayCount':4,'attachment':{'sizeBytes':len(vb),'sha256':vh,'matchesState':True},'positiveReplies':0,'replyComplete':True,'roleQualifiedMarkets':['Australia','United States'],'heldMarkets':['New Zealand','Canada']})
p['lastRunAt']=stamp;write(pp,p)
lock=root/'docs/market/us/daily/SEND-LOCK.json'
with lock.open('x',encoding='utf-8') as fh:json.dump({'runId':d['runId'],'startedAt':stamp,'runDirectory':rel(rd)},fh)
s['activeSendingRun']={'runId':d['runId'],'startedAt':stamp,'manifest':rel(cp),'lock':rel(lock)}
s.setdefault('scheduledRuns',[]).append({'runId':d['runId'],'startedAt':d['startedAt'],'localDate':d['date'],'status':'preparing_sends','markets':{c['market']:{'organisation':c['organisation'],'status':'ready' if c['sendEligible'] else 'held','holdReasons':c['holdReasons'],'messageIds':[]}for c in d['candidates']},'sentCount':0,'confirmedMessageIds':[],'manifest':rel(cp),'report':rel(rd/'run-report.json')})
s['lastReplyCheckAt']=d['evidence']['initial']['at'];s['lastReplyCheckComplete']=True;s['lastSuccessfulReplyCheckAt']=d['evidence']['initial']['at'];s['lastReplyCheckEvidence']=rel(rd/'reply-and-sent-checks.json')
write(cp,campaign);write(sp,s)
print(json.dumps({'runId':d['runId'],'ready':[(c['market'],c['buyerContact'])for c in d['candidates']if c['sendEligible']],'held':[(c['market'],c['holdReasons'])for c in d['candidates']if not c['sendEligible']],'attachmentVerified':True,'lockCreated':True}))
