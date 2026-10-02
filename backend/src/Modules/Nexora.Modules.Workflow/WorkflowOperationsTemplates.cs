using Nexora.BuildingBlocks.Persistence;
namespace Nexora.Modules.Workflow;

public sealed record WorkflowTemplateOptions(Guid? ReviewerId=null,Guid? EscalationUserId=null,int ReminderDays=3,int BeforeExpiryDays=30,int GraceDays=7,Guid? EmailConnectorId=null,string? Recipient=null);
public sealed record WorkflowOperationTemplate(string Key,string Name,string Description,WorkflowGraph Graph);
public static class WorkflowOperationsTemplates
{
 public static readonly string[] Keys=["application-review","missing-information","member-onboarding","renewal-preparation","renewal-reminders","expiry-lapse","task-escalation","unpaid-invoice","renewal-completion","cancellation-review","reinstatement-review","contact-follow-up","invoice-posting"];
 public static WorkflowOperationTemplate[] All(Guid actor,WorkflowTemplateOptions? options=null)=>Keys.Select(key=>Build(key,actor,options??new())).ToArray();
 public static WorkflowOperationTemplate Build(string key,Guid actor,WorkflowTemplateOptions config)
 {
  var reviewer=config.ReviewerId??actor;var escalation=config.EscalationUserId??reviewer;
  var variables=new Dictionary<string,string>{{"reviewerId",reviewer.ToString()},{"escalationUserId",escalation.ToString()}};
  var nodes=new List<WorkflowNode>();string entity="members",trigger="manual",name="",description="";WorkflowScheduleConfig? schedule=null;WorkflowPredicate? filter=null;
  void Add(WorkflowNode n)=>nodes.Add(n);
  WorkflowNode Action(string id,string label,string action,string next,params (string,string)[] input)=>new(id,"action",label,Next:next,ActionKey:action,Inputs:input.ToDictionary(p=>p.Item1,p=>p.Item2));
  WorkflowNode Task(string id,string label,string next,string owner="reviewerId",string detail="")=>Action(id,label,"work.task.create",next,("title",label+": {{record.label}}"),("description",detail),("assigneeUserId","{{variables."+owner+"}}"),("dueInDays",config.ReminderDays.ToString()),("priority",owner=="escalationUserId"?"high":"normal"));
  WorkflowNode Notify(string id,string label,string next)=>Action(id,label,"work.notification.send",next,("userId","{{variables.reviewerId}}"),("message",label+": {{record.label}}"));
  WorkflowNode Inspect(string id,string action,string keyName,string next)=>Action(id,"Check current prerequisites",action,next,(keyName,"{{record.id}}"));
  WorkflowNode If(string id,string field,string op,string value,string yes,string no)=>new(id,"condition","Recheck current state",IfTrue:yes,IfFalse:no,Predicate:new(Field:field,Operator:op,Value:value));
  WorkflowNode Approval(string permission,string yes,string no)=>new("approval","approval","Review and record an explicit decision",IfTrue:yes,IfFalse:no,ApproverUserId:"{{variables.reviewerId}}",RequiredPermission:permission,TimeoutMinutes:config.ReminderDays*1440,TimeoutNext:"escalate");
  void Escalate()=>Add(Task("escalate","Resolve workflow exception","end","escalationUserId","Review the workflow history and current business state before changing records."));
  void Communicate(string id,string label,string next){if(config.EmailConnectorId!=null)Add(Action(id,label,"communication.email.send",next,("connectorId",config.EmailConnectorId.Value.ToString()),("recipient",config.Recipient!),("subject",label),("body",label+" for {{record.label}}. Please sign in to your member portal for current details.")));else Add(Notify(id,label+" — staff follow-up",next));}
  switch(key)
  {
   case "application-review":
    entity="applications";trigger="record.created";name="Application review and decision";filter=new(Field:"status",Operator:"equals",Value:"submitted");
    Communicate("acknowledge","Application received","inspect");
    Add(Inspect("inspect","membership.application.inspect","applicationId","ready"));Add(If("ready","steps.inspect.ready","equals","true","task","escalate"));
    Add(Task("task","Review membership application","approval",detail:"Confirm applicant authority for organisation applications. Main Contact/Main Account flags are not portal access grants."));
    Add(Approval("membership.decide","recheck","reject"));Add(Inspect("recheck","membership.application.inspect","applicationId","still-ready"));Add(If("still-ready","steps.recheck.ready","equals","true","approve","escalate"));
    Add(Action("approve","Approve application","membership.application.decide","confirm",("applicationId","{{record.id}}"),("decision","approve"),("reason","{{steps.approval.reason}}")));
    Add(Action("reject","Reject application","membership.application.decide","confirm",("applicationId","{{record.id}}"),("decision","reject"),("reason","{{steps.approval.reason}}")));Communicate("confirm","Application decision recorded","end");Escalate();break;
   case "missing-information":
    entity="applications";trigger="record.created";name="Missing-information follow-up";filter=new(Field:"status",Operator:"equals",Value:"submitted");
    Add(Inspect("inspect","membership.application.inspect","applicationId","ready"));Add(If("ready","steps.inspect.ready","equals","true","end","request"));
    Add(Task("request","Request missing application information","message",detail:"{{steps.inspect.missingInformation}}"));Communicate("message","Application information required","await");
    Add(new("await","await-condition","Wait for application prerequisites",Next:"end",ActionKey:"membership.application.inspect",Inputs:new(){{"applicationId","{{record.id}}"}},Predicate:new(Field:"steps.await.ready",Operator:"equals",Value:"true"),TimeoutMinutes:config.ReminderDays*1440,TimeoutNext:"still-submitted"));
    Add(If("still-submitted","status","equals","submitted","remind","end"));Communicate("remind","Application information reminder","escalate");Escalate();break;
   case "member-onboarding":
    name="Member onboarding";trigger="record.created";filter=new(Field:"status",Operator:"equals",Value:"active");Add(Task("task","Prepare member onboarding","welcome",detail:"Explain portal access through existing entitlement rules, membership benefits and next steps."));Communicate("welcome","Welcome to your membership","end");break;
   case "renewal-preparation":
    name="Renewal preparation";trigger="scheduled";schedule=new(Mode:"relative",AnchorField:"endsAtUtc",OffsetDays:-config.BeforeExpiryDays);filter=new(Field:"status",Operator:"equals",Value:"active");
    Add(Inspect("inspect","membership.member.inspect-renewal","membershipId","ready"));Add(If("ready","steps.inspect.eligible","equals","true","task","requirements"));Add(Task("task","Prepare eligible renewal","end"));Add(Task("requirements","Resolve renewal requirements","end",detail:"{{steps.inspect.requirements}}"));break;
   case "renewal-reminders":
    name="Renewal reminders";trigger="scheduled";schedule=new(Mode:"relative",AnchorField:"endsAtUtc",OffsetDays:-config.BeforeExpiryDays);filter=new(Field:"status",Operator:"equals",Value:"active");
    Add(new("anchor","variable","Remember this membership term",Next:"first",OutputVariable:"termEnd",Value:"{{record.endsAtUtc}}"));Communicate("first","Membership renewal approaching","wait");Add(new("wait","wait","Wait before reminding",Next:"current",DelayMinutes:config.ReminderDays*1440));
    Add(new("current","condition","Stop after renewal or cancellation",IfTrue:"inspect",IfFalse:"end",Predicate:new(All:[new(Field:"status",Operator:"equals",Value:"active"),new(Field:"endsAtUtc",Operator:"equals",Value:"{{variables.termEnd}}")])));
    Add(Inspect("inspect","membership.member.inspect-renewal","membershipId","eligible"));Add(If("eligible","steps.inspect.eligible","equals","true","remind","escalate"));Communicate("remind","Membership renewal reminder","end");Escalate();break;
   case "expiry-lapse":
    name="Expiry and lapse after grace period";trigger="scheduled";schedule=new(Mode:"relative",AnchorField:"endsAtUtc",OffsetDays:config.GraceDays);filter=new(Field:"status",Operator:"equals",Value:"active");
    Add(new("check","condition","Recheck actual expiry",IfTrue:"lapse",IfFalse:"end",Predicate:new(All:[new(Field:"status",Operator:"equals",Value:"active"),new(Field:"endsAtUtc",Operator:"less-or-equal",Value:"{{addDays(now, -"+config.GraceDays+")}}")])));
    Add(Action("lapse","Apply lapse command","membership.member.transition","notice",("membershipId","{{record.id}}"),("transition","lapse"),("reason","Term expired and configured grace period elapsed")));Communicate("notice","Membership has lapsed","end");break;
   case "task-escalation":
    entity="tasks";name="Task deadline and escalation";trigger="scheduled";schedule=new(Mode:"relative",AnchorField:"dueAtUtc");filter=new(All:[new(Field:"status",Operator:"not-equals",Value:"done"),new(Field:"status",Operator:"not-equals",Value:"cancelled")]);
    Add(Action("remind","Remind current assignee","work.notification.send","await",("userId","{{coalesce(record.assigneeUserId, variables.reviewerId)}}"),("message","Task is due: {{record.label}}")));
    Add(new("await","await-condition","Wait for task completion",Next:"end",Predicate:new(Any:[new(Field:"status",Operator:"equals",Value:"done"),new(Field:"status",Operator:"equals",Value:"cancelled")]),TimeoutMinutes:config.ReminderDays*1440,TimeoutNext:"escalate"));Escalate();break;
   case "unpaid-invoice":
    entity="invoices";name="Outstanding invoice follow-up";trigger="scheduled";schedule=new(Mode:"relative",AnchorField:"createdAtUtc",OffsetDays:config.ReminderDays);
    Add(Inspect("inspect","finance.invoice.inspect","invoiceId","unpaid"));Add(If("unpaid","steps.inspect.settled","equals","true","end","remind"));Communicate("remind","Outstanding invoice reminder","await");
    Add(new("await","await-condition","Wait for actual settlement",Next:"end",ActionKey:"finance.invoice.inspect",Inputs:new(){{"invoiceId","{{record.id}}"}},Predicate:new(Field:"steps.await.settled",Operator:"equals",Value:"true"),TimeoutMinutes:config.ReminderDays*1440,TimeoutNext:"escalate"));Escalate();break;
   case "renewal-completion":
    name="Renewal completion";Add(Inspect("inspect","membership.member.inspect-renewal","membershipId","eligible"));Add(If("eligible","steps.inspect.eligible","equals","true","approval","escalate"));Add(Approval("membership.manage","renew","end"));
    Add(Action("renew","Execute authorised renewal","membership.member.transition","confirm",("membershipId","{{record.id}}"),("transition","renew"),("reason","{{steps.approval.reason}}")));Communicate("confirm","Membership renewed","end");Escalate();break;
   case "cancellation-review":case "reinstatement-review":
    name=key=="cancellation-review"?"Cancellation review":"Reinstatement review";Add(Approval("membership.manage","transition","end"));Add(Action("transition","Execute authorised membership transition","membership.member.transition","confirm",("membershipId","{{record.id}}"),("transition",key=="cancellation-review"?"cancel":"reinstate"),("reason","{{steps.approval.reason}}")));Communicate("confirm",name+" completed","end");Escalate();break;
   case "contact-follow-up":
    entity="contacts";name="New Contact follow-up";trigger="record.created";Add(Action("assign","Assign record owner","crm.record.update","task",("recordId","{{record.id}}"),("ownerUserId","{{variables.reviewerId}}")));Add(Task("task","Review new Contact and missing information","end"));break;
   case "invoice-posting":
    entity="orders";name="Invoice preparation and posting";Add(Approval("finance.post","post","end"));Add(Action("post","Post through Finance command","finance.order.post","end",("orderId","{{record.id}}")));Escalate();break;
   default:throw new System.ComponentModel.DataAnnotations.ValidationException("Choose a supported operations template.");
  }
  Add(new("end","end","Complete"));description="Review the generated draft before activation. "+(config.EmailConnectorId==null?"Communications create staff follow-up notifications; configure email in this template to send member messages.":"Member messages use the selected approved SMTP connector and recipient expression.");
  return new(key,name,description,new(2,entity,trigger,nodes[0].Id,nodes.ToArray(),schedule,filter,variables));
 }
}
