import json
from pathlib import Path
from datetime import datetime,timezone,timedelta

root=Path(__file__).resolve().parents[6];rd=Path(__file__).resolve().parent
rel=lambda p:str(p.relative_to(root)).replace('\\','/')
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):
 t=p.with_suffix(p.suffix+'.tmp');t.write_text(json.dumps(d,indent=2,ensure_ascii=False)+'\n',encoding='utf-8');t.replace(p)
d=read(rd/'run-input.json');e=read(rd/'outcome-evidence.json')
sp=root/'docs/market/us/daily/STATE.json';pp=root/'docs/market/acquisition/PIPELINE.json';cp=root/'docs/market/us/outreach'/d['date']/'campaign.json'
s=read(sp);p=read(pp);campaign=read(cp);now=datetime.now(timezone.utc);stamp=now.isoformat();local=now.astimezone(timezone(timedelta(hours=1)))
assert s.get('activeSendingRun',{}).get('runId')==d['runId']
assert s['campaignStatus']=='active' and len(s['previouslyContacted'])==59
assert e['completion']['complete'] and not e['completion']['newReplies']
assert all(x['complete'] and not x['emails'] for x in e['reconciliation'])
reason='MCP tool call requires approval, but approval policy is never'
assert e['sendAttempt']['result']['isError'] and reason in str(e['sendAttempt']['result'])
assert not any(x.get('status') in ['sent','sending','unknown','send_unknown'] for x in campaign['messages'])
assert next(x for x in campaign['messages']if x.get('to')=='sales@gsrestaurantequipment.com')['status']=='failed'
assert next(x for x in campaign['messages']if x.get('to')=='marketing@jaeaust.com.au')['status']=='held'
contacts_before=json.dumps(s['previouslyContacted'],sort_keys=True);hourly_before=json.dumps(s['hourlyRuns'],sort_keys=True)
old=s.get('senderBlock',{})
s.setdefault('senderBlockHistory',[]).append({'at':stamp,'event':'Current live plugin permission and scheduled action rechecked; previous denial retained as history','previous':old})
s['senderBlock']={**old,'active':True,'at':e['sendAttempt']['at'],'reason':reason,'action':e['permissionFindings']['recommendedUserAction'],'recommendationLimit':e['permissionFindings']['recommendationLimit'],'notified':True,'scope':'current scheduled session send action','confirmedUnsentRecipient':'sales@gsrestaurantequipment.com','evidence':rel(rd/'outcome-evidence.json'),'sendAttempts':1,'pluginPermissionReportedAllowAll':True,'pluginPermissionVerifiedAt':d['checkedAt'],'officialPermissionSources':e['officialSources']}
s.setdefault('sendFailures',[]).append({'runId':d['runId'],'at':e['sendAttempt']['at'],'market':'United States','recipient':'sales@gsrestaurantequipment.com','reason':reason,'attempts':1,'acceptedMessageId':None,'sentReconciliationComplete':True,'confirmedAbsentInSent':True})
outcomes={}
for c in d['candidates']:
 o=next(x for x in p['opportunities']if x.get('domain')==c['domain'])
 attempted=c['market']=='United States'
 if c['sendEligible']:
  o.update({'stage':'held_sender_approval','qualificationEligible':True,'sendEligible':False,'holdReasons':['Current scheduled-session denial: '+reason],'outreachDecision':'held_actual_current_send_denial','nextAction':'Resolve scheduled chat approval handling, then repeat full reply, Sent, suppression, route and cap checks before any first-pitch attempt.'})
  o.setdefault('history',[]).append({'at':stamp,'event':('One actual US send action denied; reconciled absent in Sent.' if attempted else 'No Australian send action after current US action denial; confirmed absent in Sent.')+' No retry or channel/account switch.'})
  status='failed_unsent_permission' if attempted else 'held_current_permission_denial'
 else:status='held_recipient_route'
 outcomes[c['market']]={'organisation':c['organisation'],'status':status,'preparedRecipient':c.get('buyerContact'),'attemptedRecipient':c.get('buyerContact')if attempted else None,'sendToolCalls':1 if attempted else 0,'messageIds':[],'holdReasons':o['holdReasons']}
run=next(x for x in s['scheduledRuns']if x['runId']==d['runId'])
run.update({'completedAt':stamp,'status':'send_permission_denied_confirmed_unsent','markets':outcomes,'sentCount':0,'sendToolCalls':1,'confirmedMessageIds':[],'verification':{'initialReplyComplete':True,'completionReplyComplete':True,'priorExternalIdsVerified':59,'liveSentIncludingSelfTest':60,'newPositiveReplies':0,'newBounces':0,'reconciliationComplete':True,'deniedSendAbsentInSent':True,'pluginAllowAllVerified':True}})
s.update({'lastReplyCheckAt':e['completion']['at'],'lastReplyCheckComplete':True,'lastSuccessfulReplyCheckAt':e['completion']['at'],'lastReplyCompletionEvidence':rel(rd/'completion-checks.json'),'lastReplyCheckFailure':None,'lastCampaignRunAt':stamp,'activeSendingRun':None})
p['lastRunAt']=stamp;write(pp,p);write(rd/'completion-checks.json',e['completion'])
report={**run,'automationId':'nexora-daily-us-sales-outreach','timezone':'Europe/London','localStartedAt':'2026-10-13T03:30:39.188+01:00','localCompletedAt':local.isoformat(),'currentPolicy':'docs/market/MULTI-MARKET-CAMPAIGN.md','localGuards':read(rd/'pre-send-guards.json'),'actualSendFailure':e['sendAttempt'],'permissionFindings':e['permissionFindings'],'officialPermissionSources':e['officialSources'],'retryPolicy':'Denied action not retried; Sent checked. No account/channel switch.','attachmentVerification':read(rd/'draft-verification.json'),'weeklyReview':{'performed':False,'lastDate':p['lastWeeklyCohortReviewDate'],'nextDue':p['nextWeeklyCohortReviewDate']},'proofDeliverables':['Dedicated CMS demonstration outstanding','Dedicated supported portal demonstration outstanding','Truthful public product page unpublished'],'partnerResearch':{'queuePreserved':True,'relationshipUnknown':True,'messagesSent':0},'newMiddleEastPitches':0,'notification':'NOTIFY: actionable current diagnosis, Gmail already Allow all actions but scheduled chat policy never rejects required connector approval. Suggest current chat Approve for me when available; no bypass.'}
write(rd/'run-report.json',report)
notes=['# Four-country sources and decisions','',local.isoformat()+' Europe/London; zero accepted messages.','']
for c in d['candidates']:
 notes+=['## '+c['market']+': '+c['organisation'],c['observedFact'],'Hypothesis: '+c['problemHypothesis'],'Contact route: '+c['outreachLegalBasis'],'']
 for x in c.get('needEvidence',[])+c.get('buyerContactEvidence',[])+c.get('countryRulesEvidence',[])+c.get('publicationNoticeChecks',[]):notes.append('- '+x['url']+' | checked '+str(x.get('checkedAt'))+' | '+x.get('observation',x.get('error','Notice inspection complete: '+str(x.get('complete')))))
 notes+=['']
notes+=['## Current send-access diagnosis','Live Gmail plugin permission: Allow all actions. Current session approval policy: never. One actual qualified US first pitch rejected before acceptance. No automatic-review decision occurred.','Official docs: '+', '.join(e['officialSources']),e['permissionFindings']['recommendedUserAction'],e['permissionFindings']['recommendationLimit'],'No setting modified, bypass attempted or alternate sender used.','']
(rd/'sources.md').write_text('\n'.join(notes)+'\n',encoding='utf-8')
(rd/'run-report.md').write_text('# Four-country run - 13 October 2026\n\n'+local.strftime('%H:%M')+' Europe/London. Zero new emails accepted. One qualified US first-pitch action rejected because connector approval was required while the scheduled session uses approval policy never. Sent confirms no email to G&S or JAE. Gmail Allow all actions verified live; plugin permission is not the missing setting. Eligible approval requests require compatible session approval handling. Official permission documentation and current error are saved.\n\nGlobal domain, Nexora, delivery/spam/trash, Sent and all 59 associated thread checks completed before sending and at completion; actual BAFSA refusal body read. No new interest, opt-out or bounce. All 59 prior external messages remain confirmed.\n\nAU JAE and US G&S sources/contact routes reconfirmed; both MIME preparations contain matching MP4 size/hash. NZ North due review: no published relevant email, contact page form unused. CA Prism due review: privacy notice unavailable and specific CMS recipient role unresolved. No recipient slot transferred.\n\nPipeline history preserved; expired interval holds removed for reviewed unsent prospects. Weekly review next due 17 October; dedicated CMS/portal demonstrations and public product page still outstanding. No follow-ups, replies, meetings, forms, partner messages, paid work, deployment, application/sibling edits or commit/push.\n',encoding='utf-8')
assert contacts_before==json.dumps(s['previouslyContacted'],sort_keys=True)
assert hourly_before==json.dumps(s['hourlyRuns'],sort_keys=True)
write(sp,s)
lock=root/'docs/market/us/daily/SEND-LOCK.json';assert read(lock)['runId']==d['runId'];lock.unlink()
print(json.dumps({'runId':d['runId'],'newSends':0,'sendCalls':1,'newPositiveReplies':0,'replyComplete':True,'senderBlockActive':True,'historyCount':len(s['previouslyContacted']),'todaySent':0,'pendingUnknownOutcomes':0,'lockReleased':not lock.exists(),'report':rel(rd/'run-report.json')}))
