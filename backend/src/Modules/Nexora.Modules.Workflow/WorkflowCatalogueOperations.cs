using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
namespace Nexora.Modules.Workflow;
public static partial class WorkflowModule
{
 private static void AddCatalogueInspection(IServiceCollection services)
 {
  services.AddScoped<IWorkflowActionHandler>(sp=>new WorkflowInspectionAction(new("workflow.operations.inspect","Count current operational exceptions","workflows.history",["recordId"],[new("recordId","Report anchor","reference",true)],["hasExceptions","summary"],AdditionalPermissions:["membership.read","finance.read","crm.read","content.read","events.read","engagement.read","work.read"]),"recordId",async(id,ct)=>{
   var sources=sp.GetServices<IWorkflowEntitySource>();var now=DateTime.UtcNow;
   async Task<int> Count(string key,WorkflowPredicate predicate)=>(await sources.Single(x=>x.Metadata.Key==key).QueryAsync(predicate,1,ct)).Total;
   var applications=await Count("applications",new(All:[new(Field:"status",Operator:"equals",Value:"submitted"),new(Field:"createdAtUtc",Operator:"less-or-equal",Value:now.AddDays(-7).ToString("O"))]));var renewals=await Count("members",new(All:[new(Field:"status",Operator:"equals",Value:"active"),new(Field:"endsAtUtc",Operator:"less-or-equal",Value:now.AddDays(30).ToString("O"))]));
   var invoices=await sources.Single(x=>x.Metadata.Key=="invoices").QueryAsync(null,1000,ct);Check(invoices.Total<=1000,"Narrow invoice reporting before processing more than 1,000 invoices.");var inspection=sp.GetServices<IWorkflowActionHandler>().Single(x=>x.Descriptor.Key=="finance.invoice.inspect");var outstanding=0;foreach(var invoice in invoices.Items){var balance=await inspection.ExecuteDetailedAsync(new(Guid.Empty,"report",Guid.Empty,Guid.Empty,invoice.Label,invoice.Id),new(){{"invoiceId",invoice.Id.ToString()}},ct);if(balance.Outputs.GetValueOrDefault("settled")=="false")outstanding++;}
   var db=sp.GetRequiredService<WorkflowDbContext>();var stale=now.AddMinutes(-10);var failures=await db.Set<WorkflowRun>().CountAsync(x=>x.Status=="failed"||(x.Status=="queued"&&x.UpdatedAtUtc<stale)||(x.Status=="waiting"&&x.DueAtUtc<stale),ct);
   return new("Operational exception counts checked",new(){{"hasExceptions",applications+renewals+outstanding+failures>0?"true":"false"},{"summary",$"Applications submitted at least 7 days ago: {applications}; active memberships expiring within 30 days or overdue: {renewals}; outstanding invoices: {outstanding}; failed/overdue processing runs: {failures}."}});
  }));
 }
 private static void MapCatalogue(Microsoft.AspNetCore.Routing.RouteGroupBuilder api)
 {
  api.MapGet("/catalogue",async(WorkflowDbContext db,IRequestIdentity who,WorkflowExpressions expressions,Microsoft.AspNetCore.Http.HttpContext http,CancellationToken ct)=>{
   var definitions=await db.Set<WorkflowDefinition>().AsNoTracking().ToDictionaryAsync(x=>x.Id,ct);return WorkflowOperationsTemplates.All(who.UserId!.Value).Where(t=>Permissions(http).Contains(expressions.Metadata(t.Graph.EntityKey)?.Permission??"unavailable")).Select(t=>{var id=CatalogueId(who.TenantId!.Value,t.Key);definitions.TryGetValue(id,out var row);return new{t.Key,t.Name,definitionId=row?.Id,saved=row!=null&&!row.IsArchived,active=row?.ActiveVersionId!=null,requiredVariables=t.Graph.RequiredVariables??[]};});
  });
  api.MapPost("/catalogue/install",async(WorkflowDbContext db,IRequestIdentity who,WorkflowExpressions expressions,WorkflowValidation validation,Microsoft.AspNetCore.Http.HttpContext http,CancellationToken ct)=>{
   var installed=new List<object>();var skipped=new List<string>();
   var templates=WorkflowOperationsTemplates.All(who.UserId!.Value).ToList();templates.Add(WorkflowOperationsTemplates.Build("contact-follow-up",who.UserId.Value,new(RecordKind:"accounts")) with{Key="account-follow-up"});templates.Add(WorkflowOperationsTemplates.Build("data-quality",who.UserId.Value,new(RecordKind:"accounts")) with{Key="account-data-quality"});templates.Add(WorkflowOperationsTemplates.Build("event-reminders",who.UserId.Value,new(EventChanges:true)) with{Key="event-changes"});templates.Add(WorkflowOperationsTemplates.Build("refund-credit",who.UserId.Value,new(Values:new(){{"operation","refund"}})) with{Key="refund-recording"});templates.Add(WorkflowOperationsTemplates.Build("community-membership",who.UserId.Value,new(Values:new(){{"operation","leave"}})) with{Key="community-leave"});
   foreach(var template in templates){var id=CatalogueId(who.TenantId!.Value,template.Key);var existing=await db.Set<WorkflowDefinition>().SingleOrDefaultAsync(x=>x.Id==id,ct);if(existing!=null){installed.Add(new{template.Key,id,created=false,archived=existing.IsArchived});continue;}if(!Permissions(http).Contains(expressions.Metadata(template.Graph.EntityKey)?.Permission??"unavailable")){skipped.Add(template.Key);continue;}
    var errors=await validation.ValidateAsync(template.Graph,Permissions(http),ct,false);if(errors.Length>0){skipped.Add(template.Key+": "+string.Join(" ",errors));continue;}
    var row=new WorkflowDefinition{Id=id,TenantId=who.TenantId!.Value,Name=template.Name,Description=template.Description,DraftJson=WorkflowValidation.Serialize(template.Graph)};db.Add(row);Audit(db,who,id,"catalogue.draft-installed",template.Key);installed.Add(new{template.Key,id,created=true,archived=false});
   }
   try{await db.SaveChangesAsync(ct);}catch(DbUpdateException){db.ChangeTracker.Clear();throw new System.ComponentModel.DataAnnotations.ValidationException("The catalogue changed concurrently. Refresh and retry; existing definitions are preserved.");}return Microsoft.AspNetCore.Http.Results.Ok(new{installed,skipped,activation="inactive drafts"});
  }).RequireAuthorization("workflows.create");
 }
 private static Guid CatalogueId(Guid tenant,string key)=>WorkflowProcessor.ActionId(tenant,"catalogue:v1:"+key);
}
