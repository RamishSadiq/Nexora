using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;
namespace Nexora.Modules.Membership;
public static partial class MembershipModule
{
 private static void AddWorkflowInspections(IServiceCollection services)
 {
  services.AddScoped<IWorkflowActionHandler>(sp=>new WorkflowInspectionAction(new("membership.application.inspect","Check application and organisation prerequisites","membership.read",["applicationId"],[new("applicationId","Application","reference",true)],["ready","missingInformation","organisationAuthorityRequiresReview","existingActiveMembership"]),"applicationId",async(id,ct)=>{
   var db=sp.GetRequiredService<MembershipDbContext>();var crm=sp.GetRequiredService<ICrmDirectory>();var row=await db.Set<MembershipApplication>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();var issues=new List<string>();
   if(row.Status!="submitted")issues.Add("Application already decided");if(await crm.FindActiveAsync(row.ContactId,ct)==null)issues.Add("Active Contact required");if(!await db.Set<MembershipProduct>().AnyAsync(x=>x.Id==row.ProductId&&x.IsActive,ct))issues.Add("Active membership offering required");if(!row.TermsAccepted)issues.Add("Terms acceptance required");
   if(row.ApplicationType=="corporate"&&row.AccountId==null)issues.Add("Organisation required");
   if(row.AccountId!=null){if(await crm.FindActiveAsync(row.AccountId.Value,ct)==null)issues.Add("Active organisation required");if(!await crm.HasCurrentAccountContactAsync(row.AccountId.Value,row.ContactId,ct))issues.Add("Current organisation relationship required");if(string.IsNullOrWhiteSpace(row.ApplicantRole))issues.Add("Applicant role required for authority review");}
   var exists=await db.Set<MemberSubscription>().AnyAsync(x=>(row.AccountId!=null?x.AccountId==row.AccountId:x.AccountId==null&&x.ContactId==row.ContactId)&&x.Status=="active",ct);if(exists)issues.Add("Existing active membership must be resolved");
   return new("Application prerequisites checked",new(){{"ready",issues.Count==0?"true":"false"},{"missingInformation",string.Join("; ",issues)},{"organisationAuthorityRequiresReview",row.AccountId!=null?"true":"false"},{"existingActiveMembership",exists?"true":"false"}});
  }));
  services.AddScoped<IWorkflowActionHandler>(sp=>new WorkflowInspectionAction(new("membership.member.inspect-renewal","Check current renewal eligibility","membership.read",["membershipId"],[new("membershipId","Membership","reference",true)],["eligible","requirements","quotedTotal","currency","endsAtUtc"]),"membershipId",async(id,ct)=>{
   var db=sp.GetRequiredService<MembershipDbContext>();var member=await db.Set<MemberSubscription>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();try{if(member.Status is not ("active" or "lapsed"))throw new ValidationException("Membership is no longer renewable");var quote=await MembershipOperations.RenewalQuoteAsync(db,member,false,ct);return new("Renewal eligible",new(){{"eligible","true"},{"requirements",""},{"quotedTotal",quote.Total.ToString(CultureInfo.InvariantCulture)},{"currency",quote.Currency},{"endsAtUtc",member.EndsAtUtc.ToString("O")}});}catch(ValidationException e){return new("Renewal requires review",new(){{"eligible","false"},{"requirements",e.Message},{"quotedTotal",null},{"currency",member.Currency},{"endsAtUtc",member.EndsAtUtc.ToString("O")}});}
  }));
 }
}
