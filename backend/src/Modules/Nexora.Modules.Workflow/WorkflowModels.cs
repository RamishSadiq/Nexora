using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Domain;
namespace Nexora.Modules.Workflow;
public sealed class WorkflowDefinition:TenantEntity
{
 public string Name{get;set;}=""; public string Description{get;set;}=""; public string DraftJson{get;set;}="";
 public bool IsArchived{get;set;} public int PublishedNumber{get;set;} public Guid? ActiveVersionId{get;set;}
}
public sealed class WorkflowVersion:TenantEntity,IAppendOnly
{
 public Guid DefinitionId{get;set;} public int Number{get;set;} public string GraphJson{get;set;}=""; public Guid PublishedBy{get;set;}
}
public sealed class WorkflowRun:TenantEntity
{
 public Guid WorkflowVersionId{get;set;} public Guid ActorId{get;set;} public Guid RecordId{get;set;} public string RecordLabel{get;set;}="";
 public string RequestKey{get;set;}=""; public string Status{get;set;}="queued"; public string CurrentNodeId{get;set;}="";
 public DateTime? DueAtUtc{get;set;} public DateTime? LeaseUntilUtc{get;set;} public DateTime? FinishedAtUtc{get;set;}
 public Guid? ParentRunId{get;set;} public Guid? RootRunId{get;set;} public string AncestryJson{get;set;}="[]"; public string TriggerJson{get;set;}="{}"; public string ContextJson{get;set;}="{}";
 public int Steps{get;set;} public int Attempts{get;set;} public string Error{get;set;}="";
}
public sealed class WorkflowStep:TenantEntity,IAppendOnly
{
 public string InputsJson{get;set;}="{}";public string OutputsJson{get;set;}="{}";public string Scope{get;set;}="";
 public Guid RunId{get;set;} public string NodeId{get;set;}=""; public string Kind{get;set;}=""; public string Outcome{get;set;}=""; public int Sequence{get;set;}
}
public sealed class WorkflowAudit:TenantEntity,IAppendOnly
{
 public Guid DefinitionId{get;set;} public Guid ActorId{get;set;} public string Action{get;set;}=""; public string Detail{get;set;}="";
}
public sealed record WorkflowGraph(int SchemaVersion,string EntityKey,string Trigger,string EntryNodeId,WorkflowNode[] Nodes,WorkflowScheduleConfig? Schedule=null,WorkflowPredicate? Filter=null,Dictionary<string,string>? Variables=null,string? TriggerField=null,string[]? RequiredVariables=null);
public sealed record WorkflowScheduleConfig(string Mode="interval",int IntervalMinutes=1440,string Time="09:00",string Timezone="UTC",int Weekday=1,DateTime? AtUtc=null,string? AnchorField=null,int OffsetDays=0,bool Singleton=false);
public sealed record WorkflowNode(string Id,string Kind,string Label,string? Next=null,string? IfTrue=null,string? IfFalse=null,string? Field=null,string? Operator=null,string? Value=null,string? ActionKey=null,Dictionary<string,string>? Inputs=null,int? DelayMinutes=null,WorkflowPredicate? Predicate=null,string? QueryEntity=null,string? OutputVariable=null,int Limit=100,string? ItemsVariable=null,string? Body=null,string[]? Branches=null,string? WorkflowVersionId=null,string OnError="stop",string? ErrorNext=null,string? WaitUntil=null,double X=0,double Y=0,string? ApproverUserId=null,string? ApproverTeamId=null,string? RequiredPermission=null,int TimeoutMinutes=1440,int PollMinutes=5,string? TimeoutNext=null);
public sealed record WorkflowSave(string Name,string? Description,WorkflowGraph Graph,Guid? Version);
public sealed record WorkflowConcurrency(Guid Version);
public sealed record WorkflowStart(Guid RecordId,string RequestKey,Dictionary<string,string>? Variables=null);
public sealed record WorkflowBulkStart(Guid[] RecordIds,string RequestKey);
public sealed record WorkflowTest(WorkflowGraph Graph,Guid RecordId,Dictionary<string,string>? Variables=null);
public sealed class WorkflowToken:TenantEntity
{
 public Guid RunId{get;set;}public Guid? ParentTokenId{get;set;}public string NodeId{get;set;}="";public string Scope{get;set;}="";public string Status{get;set;}="queued";public DateTime? DueAtUtc{get;set;}public DateTime? LeaseUntilUtc{get;set;}public string ContextJson{get;set;}="{}";public string FramesJson{get;set;}="[]";public string PendingInputsJson{get;set;}="";public string PendingNodeId{get;set;}="";public string Error{get;set;}="";public int Attempts{get;set;}public int Steps{get;set;}
}
public sealed class WorkflowSchedule:TenantEntity
{
 public Guid DefinitionId{get;set;}public Guid WorkflowVersionId{get;set;}public Guid ActorId{get;set;}public DateTime NextAtUtc{get;set;}public DateTime ActivatedAtUtc{get;set;}=DateTime.UtcNow;public DateTime? LeaseUntilUtc{get;set;}public bool IsEnabled{get;set;}=true;public string LastError{get;set;}="";
}
public sealed class WorkflowConnector:TenantEntity
{
 public string Name{get;set;}="";public string Kind{get;set;}="http";public string Endpoint{get;set;}="";public int Port{get;set;}=587;public string Username{get;set;}="";public string ProtectedSecret{get;set;}="";public string Sender{get;set;}="";public string Model{get;set;}="";public bool IsActive{get;set;}=true;
}
public sealed class WorkflowEmailTemplate:TenantEntity { public string Name{get;set;}="";public string Subject{get;set;}="";public string Body{get;set;}=""; }
public sealed class WorkflowDelivery:TenantEntity
{
 public Guid RunId{get;set;}public Guid ConnectorId{get;set;}public Guid ActorId{get;set;}public string Kind{get;set;}="";public string Status{get;set;}="pending";public string RequestJson{get;set;}="{}";public string ResultJson{get;set;}="{}";public string Error{get;set;}="";public DateTime? StartedAtUtc{get;set;}public DateTime? FinishedAtUtc{get;set;}
}
public sealed class WorkflowWebhook:TenantEntity
{
 public Guid DefinitionId{get;set;}public Guid ActorId{get;set;}public string ProtectedSecret{get;set;}="";public bool IsActive{get;set;}=true;
}
public sealed class WorkflowApproval:TenantEntity
{
 public Guid RunId{get;set;} public Guid TokenId{get;set;} public string NodeId{get;set;}=""; public string Title{get;set;}="";
 public Guid? AssignedUserId{get;set;} public Guid? AssignedTeamId{get;set;} public string RequiredPermission{get;set;}="";
 public string Status{get;set;}="pending"; public DateTime DeadlineUtc{get;set;} public Guid? DecidedBy{get;set;}
 public DateTime? DecidedAtUtc{get;set;} public string Reason{get;set;}="";
}
