using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Events.Infrastructure;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
namespace Nexora.Modules.Events;
public static partial class EventsModule
{
 private static void AddWorkflowCommands(IServiceCollection services)
 {
  services.AddScoped<IWorkflowActionHandler>(sp=>{var db=sp.GetRequiredService<EventsDbContext>();var who=sp.GetRequiredService<IRequestIdentity>();return new WorkflowCommandAction<EventsDbContext>(db,who,new("events.enrollment.create","Enroll Contact in event / course / exam","events.manage",["offeringId","contactId"],[new("offeringId","Offering","reference",true),new("contactId","Contact","reference",true)],["enrollmentId","status"]),(input,ct)=>{Check(Guid.TryParse(input.GetValueOrDefault("offeringId"),out _)&&Guid.TryParse(input.GetValueOrDefault("contactId"),out _),"Choose an offering and Contact.");return Task.CompletedTask;},async(context,input,ct)=>{var row=await Enroll(Guid.Parse(input["offeringId"]),Guid.Parse(input["contactId"]),db,sp.GetRequiredService<ICrmDirectory>(),who,new DefaultHttpContext{TraceIdentifier="workflow:"+context.RunId},context.ActionId,ct);return new("Enrollment created",new(){{"enrollmentId",row.Id.ToString()},{"status",row.Status}});});});
 }
}
