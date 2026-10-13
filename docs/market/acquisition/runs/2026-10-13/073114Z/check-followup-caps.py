import json, io, contextlib, sys
from pathlib import Path
from datetime import datetime, timezone, timedelta

rd=Path(__file__).resolve().parent;root=rd.parents[5]
source=(root/'scripts/Record-USMarketOutreach.py').read_text(encoding='utf-8-sig')
now=datetime.now(timezone.utc);stamp=now.isoformat();older=(now-timedelta(hours=6)).isoformat()
results=[]
cases=[('rolling_total',2,True,2,True,'Canada','Five-hour overall message limit'),('daily_total',22,False,2,False,'Canada','Daily overall message limit'),('market_cap',1,True,2,True,'Australia','Five-hour market organisation limit'),('remaining_capacity',0,True,2,True,'Australia',None),('unknown_followup',0,True,0,True,'Australia','Reconcile pending follow-up')]
for name,count,recent,follow_count,follow_recent,market,expected in cases:
 fixture=(rd/'cap-check-fixtures'/name).resolve();assert fixture.is_relative_to(rd.resolve())
 statepath=fixture/'docs/market/us/daily/STATE.json';statepath.parent.mkdir(parents=True,exist_ok=True)
 cp=fixture/'docs/market/us/outreach/2026-10-13/campaign.json';cp.parent.mkdir(parents=True,exist_ok=True)
 contacts=[{'messageId':'first-'+str(i),'sentAt':stamp if recent else older,'localDate':'2026-10-13','market':'Australia'if name=='market_cap'else 'New Zealand'}for i in range(count)]
 state={'campaignStatus':'active','previouslyContacted':contacts,'dailyNewOrganisationLimit':24,'cadence':'five_hourly','intervalHours':5,'activeMarkets':['Australia','United States','New Zealand','Canada'],'newOrganisationLimitPerInterval':4,'newOrganisationLimitPerMarketPerInterval':1}
 queue=[{'messageId':'follow-'+str(i),'sentAt':stamp if follow_recent else older,'localDate':'2026-10-13','status':'sent_verified'}for i in range(follow_count)]
 if name=='unknown_followup':queue=[{'status':'unknown'}]
 statepath.write_text(json.dumps(state),encoding='utf-8')
 (statepath.parent/'ONE-TIME-FOLLOWUPS.json').write_text(json.dumps({'entries':queue}),encoding='utf-8')
 cp.write_text(json.dumps({'dailyCampaign':True,'localDate':'2026-10-13','messages':[{'to':'fixture@example.invalid','market':market,'organisation':'Synthetic cap check','status':'ready'}]}),encoding='utf-8')
 oldargv=sys.argv;sys.argv=['Record-USMarketOutreach.py','--recipient','fixture@example.invalid','--status','sending','--campaign',str(cp)]
 error=None
 try:
  with contextlib.redirect_stdout(io.StringIO()):exec(compile(source,str(root/'scripts/Record-USMarketOutreach.py'),'exec'),{'__file__':str(fixture/'scripts/Record-USMarketOutreach.py'),'__name__':'__main__'})
 except RuntimeError as exc:error=str(exc)
 finally:sys.argv=oldargv
 assert (error is None if expected is None else error and expected in error),(name,error)
 results.append({'case':name,'passed':True,'result':error or 'Allowed with remaining capacity; only synthetic fixture updated'})
(rd/'cap-check-results.json').write_text(json.dumps({'cases':results,'allPassed':True,'liveCampaignModifiedByChecks':False},indent=2)+'\n',encoding='utf-8')
print(json.dumps(results))
