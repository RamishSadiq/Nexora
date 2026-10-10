using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
namespace Nexora.Modules.Membership;
public static partial class MembershipModule
{
 private static void AddWorkflowCommands(IServiceCollection services)
 {
  AddWorkflowInspections(services);
  AddWorkflowPaymentCommand(services);
  services.AddScoped<IWorkflowActionHandler>(sp=>
  {
   var db=sp.GetRequiredService<MembershipDbContext>();var who=sp.GetRequiredService<IRequestIdentity>();
   return new WorkflowCommandAction<MembershipDbContext>(db,who,new("membership.application.decide","Decide membership application","membership.decide",["applicationId","decision","reason"],[new("applicationId","Application","reference",true),new("decision","Decision","choice",true,["approve","reject"]),new("reason","Reason","text",true)],["applicationId","membershipId","status"]),(input,ct)=>{Check(Guid.TryParse(input.GetValueOrDefault("applicationId"),out _),"Choose an application.");Choice(input.GetValueOrDefault("decision",""),"approve","reject");Text(input.GetValueOrDefault("reason",""),500);return Task.CompletedTask;},async(context,input,ct)=>{var application=await db.Set<MembershipApplication>().SingleOrDefaultAsync(x=>x.Id==Guid.Parse(input["applicationId"]),ct)??throw new KeyNotFoundException();await ApplyDecision(application,new(application.Version,input["decision"],input["reason"]),db,sp.GetRequiredService<ICrmDirectory>(),sp.GetRequiredService<IPortalRecipientDirectory>(),who,new DefaultHttpContext{TraceIdentifier="workflow:"+context.RunId},ct);return new("Application decision recorded",new(){{"applicationId",application.Id.ToString()},{"membershipId",application.MembershipId?.ToString()},{"status",application.Status}});});
  });
  services.AddScoped<IWorkflowActionHandler>(sp=>
  {
   var db=sp.GetRequiredService<MembershipDbContext>();var who=sp.GetRequiredService<IRequestIdentity>();
   return new WorkflowCommandAction<MembershipDbContext>(db,who,new("membership.member.transition","Renew / cancel / lapse / reinstate membership","membership.manage",["membershipId","transition","reason"],[new("membershipId","Membership","reference",true),new("transition","Transition","choice",true,["renew","cancel","lapse","reinstate"]),new("reason","Reason","text",true)],["membershipId","status","endsAtUtc"]),(input,ct)=>{Check(Guid.TryParse(input.GetValueOrDefault("membershipId"),out _),"Choose a membership.");Choice(input.GetValueOrDefault("transition",""),"renew","cancel","lapse","reinstate");Text(input.GetValueOrDefault("reason",""),500);return Task.CompletedTask;},async(context,input,ct)=>{var member=await db.Set<MemberSubscription>().SingleOrDefaultAsync(x=>x.Id==Guid.Parse(input["membershipId"]),ct)??throw new KeyNotFoundException();await ApplyMemberChange(member,input["transition"],Text(input["reason"],500),db,who,new DefaultHttpContext{TraceIdentifier="workflow:"+context.RunId},ct);return new("Membership transition recorded",new(){{"membershipId",member.Id.ToString()},{"status",member.Status},{"endsAtUtc",member.EndsAtUtc.ToString("O")}});});
  });
 }
}
