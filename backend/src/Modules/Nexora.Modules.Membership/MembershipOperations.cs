using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
namespace Nexora.Modules.Membership;

public static class MembershipOperations
{
    public sealed record FeeQuote(DateTime StartsAtUtc, DateTime EndsAtUtc, decimal BaseFee, decimal ProcessingFee, decimal Credit, decimal Total, string Currency, decimal JoiningFee = 0);
    public static FeeQuote Quote(MembershipProduct product, MembershipPolicy? policy, DateTime start, bool joining = false, bool exemptJoiningFee = false, bool freeMembership = false)
    {
        start = DateTime.SpecifyKind(start.Date, DateTimeKind.Utc);
        var end = start.AddMonths(product.TermMonths);
        var fee = product.Rate;
        if (policy?.RenewalMode == "fixed")
        {
            end = new DateTime(start.Year, policy.YearEndMonth, policy.YearEndDay,0,0,0,DateTimeKind.Utc).AddDays(1);
            if (end <= start) end = end.AddYears(1);
            if (policy.Prorate) fee = decimal.Round(fee * (decimal)(end-start).TotalDays / (decimal)(end-end.AddYears(-1)).TotalDays,2,MidpointRounding.AwayFromZero);
        }
        var joiningFee = joining && !exemptJoiningFee ? policy?.JoiningFee ?? 0 : 0;
        var processing = policy?.ProcessingFee ?? 0;
        if (freeMembership) { fee = 0; processing = 0; joiningFee = 0; }
        var credit = Math.Min(policy?.Credit ?? 0, fee + processing + joiningFee);
        return new(start,end,fee,processing,credit,fee+processing+joiningFee-credit,product.Currency,joiningFee);
    }

    public static async Task<FeeQuote> RenewalQuoteAsync(MembershipDbContext db, MemberSubscription member, bool reinstate, CancellationToken ct)
    {
        Check(!member.IsArchived,"Restore the membership before quoting renewal or reinstatement.");
        var product = await db.Set<MembershipProduct>().SingleAsync(x=>x.Id==member.ProductId,ct);
        Check(product.IsActive,"This membership product is inactive.");
        var policy = await db.Set<MembershipPolicy>().SingleOrDefaultAsync(x=>x.ProductId==product.Id,ct);
        var review = db.Set<MembershipReview>().Local.SingleOrDefault(x=>x.MembershipId==member.Id && x.TermEndUtc==member.EndsAtUtc)
            ?? await db.Set<MembershipReview>().SingleOrDefaultAsync(x=>x.MembershipId==member.Id && x.TermEndUtc==member.EndsAtUtc,ct);
        Check(review?.ExcludedFromRenewal != true,"This membership is excluded from renewal. Update its review first.");
        Check(policy?.RequirePayment != true || member.FreeMembership == true || review?.PaymentVerified == true,"Verify payment in the renewal review first.");
        Check(policy?.RequireDeclaration != true || review?.DeclarationAccepted == true,"Record the renewal declaration first.");
        Check(policy?.RequireCompliance != true || review?.ComplianceStatus is "passed" or "exempt","Complete compliance review first.");
        return Quote(product,policy,!reinstate && member.EndsAtUtc>DateTime.UtcNow.Date ? member.EndsAtUtc : DateTime.UtcNow.Date, freeMembership: member.FreeMembership == true);
    }

    public static void MapMembershipOperations(this RouteGroupBuilder api)
    {
        api.MapGet("/products/{id:guid}/policy", async(Guid id, MembershipDbContext db,CancellationToken ct)=>
        {
            Check(await db.Set<MembershipProduct>().AnyAsync(x=>x.Id==id,ct),"Product not found.");
            return Results.Ok(await db.Set<MembershipPolicy>().SingleOrDefaultAsync(x=>x.ProductId==id,ct));
        });
        api.MapPut("/products/{id:guid}/policy",async(Guid id, PolicyRequest r,MembershipDbContext db,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
        {
            _=await db.Set<MembershipProduct>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();
            Choice(r.RenewalMode,"rolling","fixed");
            Check(r.YearEndMonth is >=1 and <=12 && r.YearEndDay>=1 && r.YearEndDay<=DateTime.DaysInMonth(2001,r.YearEndMonth),"Choose a year-end date valid in every year.");
            Check(r.ProcessingFee>=0 && r.Credit>=0 && r.ProcessingFee<100000000 && r.Credit<100000000 && decimal.Round(r.Credit,2)==r.Credit && decimal.Round(r.ProcessingFee,2)==r.ProcessingFee,"Fees and credits must be nonnegative amounts with two decimal places.");
            if(r.JoiningFee is decimal joiningFee) Check(joiningFee>=0 && joiningFee<100000000 && decimal.Round(joiningFee,2)==joiningFee,"Joining fee must be a nonnegative amount with two decimal places.");
            var policy=await db.Set<MembershipPolicy>().SingleOrDefaultAsync(x=>x.ProductId==id,ct);
            if(policy!=null && policy.Version!=r.Version) return Conflict();
            if(policy==null){Check(r.Version==null,"Reload this policy before saving.");policy=new(){TenantId=who.TenantId!.Value,ProductId=id};db.Add(policy);}
            if(r.JoiningFee.HasValue) policy.JoiningFee=r.JoiningFee.Value;
            policy.RenewalMode=r.RenewalMode;policy.YearEndMonth=r.YearEndMonth;policy.YearEndDay=r.YearEndDay;policy.Prorate=r.Prorate;policy.ProcessingFee=r.ProcessingFee;policy.Credit=r.Credit;policy.RequirePayment=r.RequirePayment;policy.RequireCompliance=r.RequireCompliance;policy.RequireDeclaration=r.RequireDeclaration;
            Audit(db,who,http,id,"policy.updated","Updated fee and renewal policy");await db.SaveChangesAsync(ct);return Results.Ok(policy);
        }).RequireAuthorization("membership.manage");
        api.MapGet("/members/{id:guid}/operations",async(Guid id,MembershipDbContext db,CancellationToken ct)=>
        {
            var member=await db.Set<MemberSubscription>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();
            var product=await db.Set<MembershipProduct>().SingleAsync(x=>x.Id==member.ProductId,ct);
            var policy=await db.Set<MembershipPolicy>().SingleOrDefaultAsync(x=>x.ProductId==member.ProductId,ct);
            return Results.Ok(new{policy,quote=Quote(product,policy,member.EndsAtUtc>DateTime.UtcNow.Date?member.EndsAtUtc:DateTime.UtcNow.Date,freeMembership:member.FreeMembership==true),review=await db.Set<MembershipReview>().SingleOrDefaultAsync(x=>x.MembershipId==id&&x.TermEndUtc==member.EndsAtUtc,ct),certificates=await db.Set<MembershipCertificate>().Where(x=>x.MembershipId==id).OrderByDescending(x=>x.CreatedAtUtc).Take(100).ToListAsync(ct),communications=await db.Set<MembershipCommunication>().Where(x=>x.MembershipId==id).OrderByDescending(x=>x.CreatedAtUtc).Take(100).ToListAsync(ct)});
        });
        api.MapPut("/members/{id:guid}/review",async(Guid id, ReviewRequest r,MembershipDbContext db,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
        {
            var member=await db.Set<MemberSubscription>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();
            if(member.Version!=r.MemberVersion)return Conflict();
            Check(!member.IsArchived,"Restore the membership before updating its operational review.");
            Choice(r.ComplianceStatus,"pending","passed","failed","exempt");
            Check(!r.PaymentVerified || !string.IsNullOrWhiteSpace(r.PaymentReference),"A finance/payment reference is required for verified payment.");
            Check(r.ComplianceStatus is "pending" || !string.IsNullOrWhiteSpace(r.Evidence),"Record evidence or an exemption reason for compliance decisions.");
            var review=await db.Set<MembershipReview>().SingleOrDefaultAsync(x=>x.MembershipId==id&&x.TermEndUtc==member.EndsAtUtc,ct);
            if(review!=null&&review.Version!=r.Version)return Conflict();
            if(review==null){Check(r.Version==null,"Reload this review before saving.");review=new(){TenantId=who.TenantId!.Value,MembershipId=id,TermEndUtc=member.EndsAtUtc};db.Add(review);}
            review.PaymentVerified=r.PaymentVerified;review.PaymentReference=Optional(r.PaymentReference,500);review.ComplianceStatus=r.ComplianceStatus;review.Evidence=Optional(r.Evidence,2000);review.DeclarationAccepted=r.DeclarationAccepted;review.ExcludedFromRenewal=r.ExcludedFromRenewal;review.Notes=Optional(r.Notes,500);
            // Touch the membership so review changes and renewal/certificate issuance cannot race silently.
            member.UpdatedAtUtc=DateTime.UtcNow;
            Audit(db,who,http,id,"review.updated",review.Notes.Length>0?review.Notes:"Updated renewal and compliance review");await db.SaveChangesAsync(ct);return Results.Ok(review);
        }).RequireAuthorization("membership.manage");
        api.MapPost("/members/{id:guid}/certificates",async(Guid id, VersionRequest r,MembershipDbContext db,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
        {
            var member=await db.Set<MemberSubscription>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();
            Check(!member.IsArchived,"Restore the membership before issuing a certificate.");
            Check(member.Status=="active"&&member.EndsAtUtc>DateTime.UtcNow,"Only an active, unexpired membership can receive a certificate.");
            var existing=await db.Set<MembershipCertificate>().SingleOrDefaultAsync(x=>x.MembershipId==id&&x.EndsAtUtc==member.EndsAtUtc,ct);
            if(existing!=null)return Results.Ok(existing);
            if(member.Version!=r.Version)return Conflict();
            var policy=await db.Set<MembershipPolicy>().SingleOrDefaultAsync(x=>x.ProductId==member.ProductId,ct);
            var review=await db.Set<MembershipReview>().SingleOrDefaultAsync(x=>x.MembershipId==id&&x.TermEndUtc==member.EndsAtUtc,ct);
            Check(policy?.RequireCompliance!=true||review?.ComplianceStatus is "passed" or "exempt","Complete compliance review before issuing a certificate.");
            Check(policy?.RequirePayment!=true||review?.PaymentVerified==true,"Verify payment before issuing a certificate.");
            var product=await db.Set<MembershipProduct>().SingleAsync(x=>x.Id==member.ProductId,ct);
            var certificate=new MembershipCertificate{TenantId=who.TenantId!.Value,MembershipId=id,Number=$"NX-{id:N}-{member.EndsAtUtc:yyyyMMdd}",HolderName=member.AccountName??member.ContactName,ProductName=product.Name,StartsAtUtc=member.StartsAtUtc,EndsAtUtc=member.EndsAtUtc,IssuedByUserId=who.UserId!.Value};
            member.UpdatedAtUtc=DateTime.UtcNow;db.Add(certificate);Audit(db,who,http,id,"certificate.issued",certificate.Number);await db.SaveChangesAsync(ct);return Results.Ok(certificate);
        }).RequireAuthorization("membership.manage");
        api.MapPost("/members/{id:guid}/communications",async(Guid id, CommunicationRequest r,MembershipDbContext db,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
        {
            var member=await db.Set<MemberSubscription>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();
            Check(!member.IsArchived,"Restore the membership before drafting communications.");
            Check(r.RequestId!=Guid.Empty,"A draft request identifier is required.");Choice(r.Kind,"welcome","renewal","payment","general");
            var recipient=Text(r.Recipient,160);var subject=Text(r.Subject,160);var body=Text(r.Body,4000);
            var existing=await db.Set<MembershipCommunication>().SingleOrDefaultAsync(x=>x.RequestId==r.RequestId,ct);
            if(existing!=null){Check(existing.MembershipId==id&&existing.Recipient==recipient&&existing.Subject==subject&&existing.Body==body&&existing.Kind==r.Kind,"This request identifier belongs to a different draft.");return Results.Ok(existing);}
            member.UpdatedAtUtc=DateTime.UtcNow;
            var draft=new MembershipCommunication{TenantId=who.TenantId!.Value,MembershipId=id,Kind=r.Kind,Recipient=recipient,Subject=subject,Body=body,RequestId=r.RequestId,CreatedByUserId=who.UserId!.Value};db.Add(draft);Audit(db,who,http,id,"communication.drafted",subject);await db.SaveChangesAsync(ct);return Results.Ok(draft);
        }).RequireAuthorization("membership.manage");
    }
    private static string Optional(string? value,int max)=>string.IsNullOrWhiteSpace(value)?"":Text(value,max);
    private static void Audit(MembershipDbContext db,IRequestIdentity who,HttpContext http,Guid id,string action,string reason)=>db.Add(new MembershipHistory{TenantId=who.TenantId!.Value,SubjectId=id,ActorUserId=who.UserId!.Value,Action=action,Reason=reason,CorrelationId=http.TraceIdentifier});
}
public sealed record PolicyRequest(Guid? Version,string RenewalMode,int YearEndMonth,int YearEndDay,bool Prorate,decimal ProcessingFee,decimal Credit,bool RequirePayment,bool RequireCompliance,bool RequireDeclaration,decimal? JoiningFee=null);
public sealed record ReviewRequest(Guid MemberVersion,Guid? Version,bool PaymentVerified,string? PaymentReference,string ComplianceStatus,string? Evidence,bool DeclarationAccepted,bool ExcludedFromRenewal,string? Notes);
public sealed record VersionRequest(Guid Version);
public sealed record CommunicationRequest(Guid RequestId,string Kind,string Recipient,string Subject,string Body);
