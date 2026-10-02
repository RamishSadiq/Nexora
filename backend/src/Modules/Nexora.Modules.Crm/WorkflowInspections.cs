using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;
namespace Nexora.Modules.Crm;
public static partial class CrmModule
{
 private static void AddWorkflowInspections(IServiceCollection services)
 {
  services.AddScoped<IWorkflowActionHandler>(sp=>new WorkflowInspectionAction(new("crm.record.inspect-quality","Review missing details and possible duplicates","crm.read",["recordId"],[new("recordId","Contact or Account","reference",true)],["needsReview","issues","duplicateCount"]),"recordId",async(id,ct)=>{
   var db=sp.GetRequiredService<CrmDbContext>();var row=await db.Records.SingleOrDefaultAsync(x=>x.Id==id&&!x.IsArchived,ct)??throw new KeyNotFoundException();var issues=new List<string>();
   if(string.IsNullOrWhiteSpace(row.Name))issues.Add("Name missing");if(row.OwnerUserId==null&&row.OwnerTeamId==null)issues.Add("Owner missing");if(!await db.Set<CommunicationMethod>().AnyAsync(x=>x.RecordId==id,ct))issues.Add("Communication details missing");
   var name=row.Name.Trim().ToUpper();var duplicates=await db.Records.CountAsync(x=>x.Id!=id&&!x.IsArchived&&x.Kind==row.Kind&&x.Name.Trim().ToUpper()==name,ct);if(duplicates>0)issues.Add(duplicates+" records have the same name; review before merging");
   return new("Data quality checked",new(){{"needsReview",issues.Count>0?"true":"false"},{"issues",string.Join("; ",issues)},{"duplicateCount",duplicates.ToString()}});
  }));
  services.AddScoped<IWorkflowActionHandler>(sp=>new WorkflowInspectionAction(new("crm.relationship.inspect","Review organisation relationship and handover","crm.read",["relationshipId"],[new("relationshipId","Organisation relationship","reference",true)],["current","accountOwnerId","contactId","accountId","issues"]),"relationshipId",async(id,ct)=>{
   var db=sp.GetRequiredService<CrmDbContext>();var row=await db.Set<AccountContact>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();var account=await db.Records.SingleAsync(x=>x.Id==row.AccountId,ct);var contact=await db.Records.SingleAsync(x=>x.Id==row.ContactId,ct);var current=row.EndDate==null&&!account.IsArchived&&!contact.IsArchived&&account.Status=="active"&&contact.Status=="active";
   return new("Relationship reviewed; no portal grant changed",new(){{"current",current?"true":"false"},{"accountOwnerId",account.OwnerUserId?.ToString()},{"contactId",row.ContactId.ToString()},{"accountId",row.AccountId.ToString()},{"issues",current?"Review role, authority and outstanding work. Main flags do not confer portal access.":"Relationship or parent is no longer current; review operational responsibility and delegated access."}});
  }));
 }
}
