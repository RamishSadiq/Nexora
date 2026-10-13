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
assert e['sendAttempt']['acceptedMessageId'] is None and e['sendAttempt']['result']['isError']
reason='MCP tool call requires approval, but approval policy is never'
assert reason in str(e['sendAttempt']['result'])
assert sum(x.get('status')=='sent' for x in campaign['messages'])==4
assert not any(x.get('status') in ['sending','unknown','send_unknown'] for x in campaign['messages'])
assert next(x for x in campaign['messages']if x.get('to')=='marketing@jaeaust.com.au')['status']=='failed'
assert next(x for x in campaign['messages']if x.get('to')=='sales@gsrestaurantequipment.com')['status']=='held'
prior_contacts=json.dumps(s['previouslyContacted'],sort_keys=True)
old=s.get('senderBlock',{})
previous_resolution={k:old.get(k)for k in ['resolvedAt','permissionUpdatedAt','resolution','confirmedRecoveryMessageIds','runtimeSendVerified']}
s['senderBlock']={'active':True,'at':e['sendAttempt']['at'],'reason':reason,'action':'Scheduled execution must support authorization for Gmail send. Plugin Allow all actions did not remove this actual session approval-policy denial. Do not bypass or switch accounts/channels.','notified':True,'scope':'current scheduled session send action','confirmedUnsentRecipient':'marketing@jaeaust.com.au','evidence':rel(rd/'outcome-evidence.json'),'sendAttempts':1,'pluginPermissionReportedAllowAll':True,'previousVerifiedResolution':previous_resolution}
s.setdefault('sendFailures',[]).append({'runId':d['runId'],'at':e['sendAttempt']['at'],'market':'Australia','recipient':'marketing@jaeaust.com.au','reason':reason,'attempts':1,'acceptedMessageId':None,'sentReconciliationComplete':True,'confirmedAbsentInSent':True})
outcomes={}
for c in d['candidates']:
 opportunity=next(x for x in p['opportunities']if x.get('domain')==c['domain'])
 if c['sendEligible']:
  opportunity.update({'stage':'held_sender_approval','sendEligible':False,'holdReasons':['Fresh scheduled-session denial: '+reason],'outreachDecision':'held_actual_current_send_denial','nextAction':'After execution-context approval is enabled, reconcile Sent and repeat complete replies, suppression, route and cap checks before retrying.'})
  opportunity.setdefault('history',[]).append({'at':stamp,'event':'One scheduled send action denied; Sent confirms no accepted message. No repeated action or channel/account bypass.'})
  status='failed_unsent_permission' if c['market']=='Australia' else 'held_current_permission_denial'
 else:status='held_recipient_route'
 outcomes[c['market']]={'organisation':c['organisation'],'status':status,'preparedRecipient':c.get('buyerContact'),'attemptedRecipient':c['buyerContact']if c['market']=='Australia'else None,'sendToolCalls':1 if c['market']=='Australia'else 0,'messageIds':[],'holdReasons':opportunity['holdReasons']}
run=next(x for x in s['scheduledRuns']if x['runId']==d['runId'])
run.update({'completedAt':stamp,'status':'send_permission_denied_confirmed_unsent','markets':outcomes,'sentCount':0,'sendToolCalls':1,'confirmedMessageIds':[],'verification':{'initialReplyComplete':True,'completionReplyComplete':True,'priorExternalIdsVerified':59,'liveSentIncludingSelfTest':60,'newPositiveReplies':0,'newBounces':0,'reconciliationComplete':True,'deniedSendAbsentInSent':True}})
s.update({'lastReplyCheckAt':e['completion']['at'],'lastReplyCheckComplete':True,'lastSuccessfulReplyCheckAt':e['completion']['at'],'lastReplyCompletionEvidence':rel(rd/'completion-checks.json'),'lastReplyCheckFailure':None,'lastCampaignRunAt':stamp,'activeSendingRun':None})
daily=next((r for r in s.get('runs',[])if r.get('date')==d['date']),None)
if daily:daily.update({'status':'completed_with_send_permission_hold','updatedAt':stamp,'sentCount':4,'messageIds':[x['messageId']for x in campaign['messages']if x.get('status')=='sent']})
assert prior_contacts==json.dumps(s['previouslyContacted'],sort_keys=True)
p['lastRunAt']=stamp;write(pp,p);write(rd/'completion-checks.json',e['completion'])
report={**run,'automationId':'nexora-daily-us-sales-outreach','timezone':'Europe/London','localStartedAt':'2026-10-12T22:32:34.430+01:00','localCompletedAt':local.isoformat(),'currentPolicy':'docs/market/MULTI-MARKET-CAMPAIGN.md','localGuards':read(rd/'pre-send-guards.json'),'actualSendFailure':e['sendAttempt'],'retryPolicy':'Denied action not retried; Sent checked. No account/channel switch.','attachmentVerification':read(rd/'draft-verification.json'),'weeklyReview':{'performed':False,'lastDate':p['lastWeeklyCohortReviewDate'],'nextDue':p['nextWeeklyCohortReviewDate']},'proofDeliverables':['Dedicated CMS demonstration outstanding','Dedicated supported portal demonstration outstanding','Truthful public product page unpublished'],'partnerResearch':{'queuePreserved':True,'newPotentialPartnerSource':'https://www.righthandman.co.nz/engineering-marketing-services','observedFact':'Official marketing services include website/content work; overlap/complement unconfirmed.','founderRelationship':None,'willingness':None,'messagesSent':0,'nextAction':'Founder may supply a real relationship before a specific introduction request; no automatic message.'},'newMiddleEastPitches':0,'notification':'NOTIFY: fresh scheduled Gmail send denial despite earlier permission recovery; zero new accepted emails.'}
write(rd/'run-report.json',report)
notes=['# Four-country sources and decisions','',local.strftime('%H:%M')+' Europe/London. One Australian send action denied before acceptance; no new sends.','']
for c in d['candidates']:
 notes+=['## '+c['market']+': '+c['organisation'],c['observedFact'],'Hypothesis: '+c['problemHypothesis'],'Contact route: '+c['outreachLegalBasis'],'']
 for x in c.get('needEvidence',[])+c.get('buyerContactEvidence',[])+c.get('countryRulesEvidence',[])+c.get('publicationNoticeChecks',[]):notes.append('- '+x['url']+' | checked '+str(x.get('checkedAt'))+' | '+x.get('observation',x.get('error','Notice inspection complete: '+str(x.get('complete')))))
 notes+=['']
(rd/'sources.md').write_text('\n'.join(notes)+'\n',encoding='utf-8')
(rd/'run-report.md').write_text('# Four-country run - 12 October 2026\n\n'+local.strftime('%H:%M')+' Europe/London. No new emails accepted. One Gmail send action to JAE Engineering Group was denied: '+reason+'. Sent confirms no message to that address or G&S. US sending held after the denial; no repeated action or alternate account/channel.\n\nInitial/completion global replies, overlap/domain searches, spam/trash/delivery checks and 59 associated conversations completed. No new interest, opt-out or delivery failure. All 59 earlier external pitches and four sends today preserved. Earlier manual permission recovery does not override this actual scheduled-session denial.\n\nAustralia: direct corporate/marketing publication, exact CMS relevance, no accompanying refusal and current Spam Act Schedule 2 conditions recorded. US: G&S showroom announcement and sales inbox reconfirmed. Both MIME files contain verified MP4. NZ EMA: marketing leader identified, exact relevant email not verified; course/career/advocacy contacts not repurposed. Canada Inline: communications email verified, but linked privacy/terms timed out, so recipient route held.\n\nWeekly review next due 17 October. Dedicated CMS/portal demos and product page remain outstanding. No follow-ups, automatic replies, meetings, partner messages, forms, paid actions, deployment, application/sibling changes or commit/push.\n',encoding='utf-8')
write(sp,s)
lock=root/'docs/market/us/daily/SEND-LOCK.json';assert read(lock)['runId']==d['runId'];lock.unlink()
print(json.dumps({'runId':d['runId'],'newSends':0,'sendCalls':1,'newPositiveReplies':0,'replyComplete':True,'senderBlockActive':True,'historyCount':len(s['previouslyContacted']),'todaySent':4,'pendingUnknownOutcomes':0,'lockReleased':not lock.exists(),'report':rel(rd/'run-report.json')}))
