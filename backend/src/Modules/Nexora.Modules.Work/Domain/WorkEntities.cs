using Nexora.BuildingBlocks.Domain;
namespace Nexora.Modules.Work.Domain;
public sealed class WorkItem:TenantEntity{public string Title{get;set;}="";public string Description{get;set;}="";public string Status{get;set;}="open";public string Priority{get;set;}="normal";public DateTime? DueAtUtc{get;set;}public Guid? AssigneeUserId{get;set;}public Guid? TeamId{get;set;}public Guid? ContactId{get;set;}}
public sealed class Notification:TenantEntity{public Guid UserId{get;set;}public string Message{get;set;}="";public Guid WorkItemId{get;set;}public bool IsRead{get;set;}}
public sealed class WorkHistory:TenantEntity,IAppendOnly{public Guid SubjectId{get;set;}public Guid ActorUserId{get;set;}public string Action{get;set;}="";public string Reason{get;set;}="";public string CorrelationId{get;set;}="";}
public sealed class ImportBatch:TenantEntity{public string Name{get;set;}="";public string Status{get;set;}="preview";public int RowCount{get;set;}public Guid CreatedByUserId{get;set;}}
public sealed class ImportRow:TenantEntity,IAppendOnly{public Guid BatchId{get;set;}public string Title{get;set;}="";public string Priority{get;set;}="normal";public DateTime? DueAtUtc{get;set;}}
