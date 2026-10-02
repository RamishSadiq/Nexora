using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Engagement.Infrastructure;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
namespace Nexora.Modules.Engagement;
public static partial class EngagementModule
{
 private static void AddWorkflowCommands(IServiceCollection services)
 {
  services.AddScoped<IWorkflowActionHandler>(sp=>{var db=sp.GetRequiredService<EngagementDbContext>();var who=sp.GetRequiredService<IRequestIdentity>();return new WorkflowCommandAction<EngagementDbContext>(db,who,new("engagement.community.remove-member","Remove community membership","engagement.manage",["communityId","memberId"],[new("communityId","Community","reference",true),new("memberId","Community membership","reference",true)],[]),(input,ct)=>{Check(Guid.TryParse(input.GetValueOrDefault("communityId"),out _)&&Guid.TryParse(input.GetValueOrDefault("memberId"),out _),"Choose a community and membership.");return Task.CompletedTask;},async(context,input,ct)=>{await RemoveCommunityMember(Guid.Parse(input["communityId"]),Guid.Parse(input["memberId"]),db,who,new DefaultHttpContext{TraceIdentifier="workflow:"+context.RunId},ct);return new("Community member removed",new());});});
  services.AddScoped<IWorkflowActionHandler>(sp=>{var db=sp.GetRequiredService<EngagementDbContext>();var who=sp.GetRequiredService<IRequestIdentity>();return new WorkflowCommandAction<EngagementDbContext>(db,who,new("engagement.community.add-member","Add Contact to community","engagement.manage",["communityId","contactId","role"],[new("communityId","Community","reference",true),new("contactId","Contact","reference",true),new("role","Role","text",true)],["memberId"]),(input,ct)=>{Check(Guid.TryParse(input.GetValueOrDefault("communityId"),out _)&&Guid.TryParse(input.GetValueOrDefault("contactId"),out _),"Choose a community and Contact.");Text(input.GetValueOrDefault("role",""),80);return Task.CompletedTask;},async(context,input,ct)=>{var row=await AddCommunityMember(Guid.Parse(input["communityId"]),Guid.Parse(input["contactId"]),input["role"],db,sp.GetRequiredService<ICrmDirectory>(),who,new DefaultHttpContext{TraceIdentifier="workflow:"+context.RunId},context.ActionId,ct);return new("Community member added",new(){{"memberId",row.Id.ToString()}});});});
 }
}
