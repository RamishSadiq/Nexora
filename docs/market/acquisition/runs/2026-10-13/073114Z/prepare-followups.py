import json, hashlib
from pathlib import Path
from datetime import datetime, timezone, timedelta

root=Path(__file__).resolve().parents[6]; rd=Path(__file__).resolve().parent
rel=lambda p:str(p.relative_to(root)).replace('\\','/')
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):
 t=p.with_suffix(p.suffix+'.tmp');t.write_text(json.dumps(d,indent=2,ensure_ascii=False)+'\n',encoding='utf-8');t.replace(p)
d=read(rd/'run-input.json');sp=root/'docs/market/us/daily/STATE.json';qp=root/'docs/market/us/daily/ONE-TIME-FOLLOWUPS.json'
s=read(sp);q=read(qp);now=datetime.now(timezone.utc);stamp=now.isoformat()
assert s['campaignStatus']=='active' and not s.get('activeSendingRun')
assert not (root/'docs/market/us/daily/SEND-LOCK.json').exists()
assert len(s['previouslyContacted'])==59 and len(q['entries'])==2
assert d['initial']['complete'] and not d['initial']['newPositiveReplies']
sent=set(d['initial']['searches'][-2]['ids']);assert all(c['messageId']in sent for c in s['previouslyContacted'])
recent=[c for c in s['previouslyContacted'] if c.get('sentAt') and datetime.fromisoformat(c['sentAt'].replace('Z','+00:00'))>now-timedelta(hours=5)]
follow_sent=[e for e in q['entries']if e.get('sentAt') and e.get('messageId')]
recent_follow=[e for e in follow_sent if datetime.fromisoformat(e['sentAt'].replace('Z','+00:00'))>now-timedelta(hours=5)]
today_first=[c for c in s['previouslyContacted']if c.get('localDate')==d['date']]
today_follow=[e for e in follow_sent if e.get('localDate')==d['date']]
assert len(recent)+len(recent_follow)+2<=4 and len(today_first)+len(today_follow)+2<=24
assert now.astimezone(timezone(timedelta(hours=1))).date().isoformat()==d['date']
sup=read(root/'docs/market/OUTREACH-SUPPRESSIONS.json')
for e in q['entries']:
 assert e['status'] in ['queued','blocked_unsent'] and not e.get('messageId')
 assert not any(x.get('email')==e['to'] or (x.get('scope')!='address_only' and x.get('domain')==e['to'].split('@')[1])for x in sup['entries'])
 v=next(x for x in d['initial']['draftVerifications']if x['draftId']==e['draftId'])
 assert v['messageId']==e['draftMessageId'] and v['subject']==e['subject'] and v['to']==e['to']
 m=next(x for x in d['initial']['drafts']if x['id']==e['draftMessageId'])
 e['lastVerifiedAt']=stamp;e['verifiedBodySHA256']=hashlib.sha256(m['body'].encode()).hexdigest();e['lastVerifiedRunId']=d['runId']
 e.setdefault('events',[]).append({'at':stamp,'status':e['status'],'reason':'Exact authorized saved draft and immutable message/thread ID, sender, recipient, subject, body, no CC/BCC, no new attachment verified. Complete global checks and no Sent body match.'})
lock=root/'docs/market/us/daily/SEND-LOCK.json'
with lock.open('x',encoding='utf-8')as f:json.dump({'runId':d['runId'],'startedAt':stamp,'queue':rel(qp)},f)
s['activeSendingRun']={'runId':d['runId'],'startedAt':stamp,'queue':rel(qp),'lock':rel(lock)}
s.setdefault('scheduledRuns',[]).append({'runId':d['runId'],'startedAt':d['startedAt'],'localDate':d['date'],'status':'verified_one_time_followups_ready','followups':[{'entryId':e['id'],'organisation':e['organisation'],'to':e['to'],'draftId':e['draftId'],'status':e['status'],'messageIds':[]}for e in q['entries']],'markets':{'United States':{'status':'no_additional_US_pitch_one_time_pair'},'Australia':{'status':'pending_oldest_eligible_JAE'},'New Zealand':{'status':'research_pending'},'Canada':{'status':'research_pending'}},'reservedFollowupSlots':2,'maximumOtherFirstPitches':2,'sentCount':0,'confirmedMessageIds':[],'report':rel(rd/'run-report.json')})
s['lastReplyCheckAt']=d['initial']['at'];s['lastSuccessfulReplyCheckAt']=d['initial']['at'];s['lastReplyCheckComplete']=True;s['lastReplyCheckEvidence']=rel(rd/'run-input.json')
write(qp,q);write(sp,s)
write(rd/'pre-send-guards.json',{'at':stamp,'rollingFirstPitches':len(recent),'rollingFollowups':len(recent_follow),'todayFirstPitches':len(today_first),'todayFollowups':len(today_follow),'reservedSlots':2,'overallRollingCap':4,'overallDailyCap':24,'historyVerified':59,'draftsVerified':2,'globalRepliesComplete':True,'unknownOutcomes':0,'noAdditionalUSFirstPitch':True})
print(json.dumps({'runId':d['runId'],'draftsReady':2,'rollingMessages':len(recent)+len(recent_follow),'todayMessages':len(today_first)+len(today_follow),'lockCreated':True}))
