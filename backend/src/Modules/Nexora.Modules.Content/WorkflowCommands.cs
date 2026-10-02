using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Content;
public static partial class ContentModule
{
 private static void AddWorkflowCommands(IServiceCollection services)
 {
  services.AddScoped<IWorkflowActionHandler>(sp=>new WorkflowInspectionAction(new("content.page.inspect-draft","Read saved draft for preview and approval","content.read",["pageId"],[new("pageId","Page","reference",true)],["revisionId","title","previewPath"]),"pageId",async(id,ct)=>{
   var db=sp.GetRequiredService<ContentDbContext>();var page=await Find(db,id,ct);var revision=await db.Versions.SingleAsync(x=>x.Id==page.DraftId&&x.PageId==id,ct);await ValidateRevisionMedia(db,page.SiteId,revision,false,ct);
   return new("Saved draft ready for preview",new(){{"revisionId",revision.Id.ToString()},{"title",revision.Title},{"previewPath","/api/v1/content/pages/"+id+"/preview"}});
  }));
  services.AddScoped<IWorkflowActionHandler>(sp=>{var db=sp.GetRequiredService<ContentDbContext>();var who=sp.GetRequiredService<IRequestIdentity>();return new WorkflowCommandAction<ContentDbContext>(db,who,new("content.page.publish","Publish the reviewed saved draft","content.publish",["pageId","revisionId"],[new("pageId","Page","reference",true),new("revisionId","Reviewed revision","reference",true)],["pageId","revisionId"]),(input,ct)=>{if(!Guid.TryParse(input.GetValueOrDefault("pageId"),out _)||!Guid.TryParse(input.GetValueOrDefault("revisionId"),out _))throw new ValidationException("Choose a page and saved revision.");return Task.CompletedTask;},async(context,input,ct)=>{
   var page=await Find(db,Guid.Parse(input["pageId"]),ct);var revision=Guid.Parse(input["revisionId"]);if(page.DraftId!=revision)throw new ValidationException("The draft changed after review. Preview and approve the new revision.");await PublishRevision(page,revision,db,who.UserId!.Value,ct);return new("Reviewed draft published",new(){{"pageId",page.Id.ToString()},{"revisionId",revision.ToString()}});
  });});
 }
 private static async Task PublishRevision(Page page,Guid revisionId,ContentDbContext db,Guid actor,CancellationToken ct)
 {
  var revision=await db.Versions.SingleOrDefaultAsync(x=>x.Id==revisionId&&x.PageId==page.Id,ct)??throw new KeyNotFoundException();await ValidateRevisionMedia(db,page.SiteId,revision,true,ct);await CancelSchedule(db,page.Id,ct);page.PublishedId=revisionId;db.Add(new Publication{TenantId=page.TenantId,PageId=page.Id,PageVersionId=revisionId,ActorId=actor});
 }
}
