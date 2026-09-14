using Nexora.BuildingBlocks.Reporting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Endpoints;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Events.Domain;
using Nexora.Modules.Events.Infrastructure;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
namespace Nexora.Modules.Events;
public static class EventsModule
{
    public static IServiceCollection AddNexoraEvents(this IServiceCollection services,IConfiguration config)
    {services.AddDbContext<EventsDbContext>(o=>o.UseSqlServer(config.GetConnectionString("Nexora"),sql=>sql.MigrationsHistoryTable("__EFMigrationsHistory","events")));services.AddScoped<IReportDataset>(sp=>new ReportDataset("offerings","Events and learning","events.read",sp.GetRequiredService<EventsDbContext>().Set<Offering>().AsNoTracking().Select(x=>new ReportRow{Id=x.Id,Label=x.Name,Status=x.Status,CreatedAtUtc=x.CreatedAtUtc})));
        return services;}
    public static IEndpointRouteBuilder MapNexoraEvents(this IEndpointRouteBuilder routes)
    {
        var api=routes.Module("events");
        api.MapGet("/contacts",(string? search,ICrmDirectory crm,CancellationToken ct)=>crm.SearchContactsAsync(search,ct));
        api.MapGet("/offerings",async(EventsDbContext db,string? kind,CancellationToken ct)=>{var q=db.Set<Offering>().AsNoTracking();if(!string.IsNullOrWhiteSpace(kind)){Choice(kind,"event","course","exam");q=q.Where(x=>x.Kind==kind);}return Results.Ok(await q.OrderBy(x=>x.StartsAtUtc).Take(200).ToListAsync(ct));});
        api.MapPost("/offerings",async(OfferingRequest r,EventsDbContext db,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
        {
            Choice(r.Kind,"event","course","exam");Check(r.Capacity is >=1 and <=100000,"Capacity must be 1–100000.");Check(r.StartsAtUtc<r.EndsAtUtc,"End must follow start.");Check(r.PassingScore is >=0 and <=100,"Passing score must be 0–100.");
            var row=new Offering{TenantId=who.TenantId!.Value,Kind=r.Kind,Name=Text(r.Name),Venue=Text(r.Venue),StartsAtUtc=r.StartsAtUtc.ToUniversalTime(),EndsAtUtc=r.EndsAtUtc.ToUniversalTime(),Capacity=r.Capacity,PassingScore=r.PassingScore};db.Add(row);History(db,who,http,row.Id,null,"offering.created","Published schedule");await db.SaveChangesAsync(ct);return Results.Ok(row);
        }).RequireAuthorization("events.manage");
        api.MapGet("/offerings/{id:guid}",async(Guid id,EventsDbContext db,CancellationToken ct)=>
        {
            var offering=await OfferingAsync(db,id,ct);return Results.Ok(new{offering,sessions=await db.Set<EventSession>().Where(x=>x.OfferingId==id).OrderBy(x=>x.StartsAtUtc).ToListAsync(ct),enrollments=await db.Set<Enrollment>().Where(x=>x.OfferingId==id).OrderBy(x=>x.ContactName).ToListAsync(ct),results=await(from result in db.Set<ExamResult>() join enrollment in db.Set<Enrollment>() on result.EnrollmentId equals enrollment.Id where enrollment.OfferingId==id select result).ToListAsync(ct),history=await db.Set<EventHistory>().Where(x=>x.OfferingId==id).OrderByDescending(x=>x.CreatedAtUtc).Take(100).ToListAsync(ct)});
        });
        api.MapPost("/offerings/{id:guid}/sessions",async(Guid id,SessionRequest r,EventsDbContext db,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
        {var offering=await OfferingAsync(db,id,ct);Check(offering.Status=="published","The offering is cancelled.");Check(r.StartsAtUtc>=offering.StartsAtUtc&&r.EndsAtUtc<=offering.EndsAtUtc&&r.StartsAtUtc<r.EndsAtUtc,"Session times must fit the offering.");var row=new EventSession{TenantId=who.TenantId!.Value,OfferingId=id,Name=Text(r.Name),StartsAtUtc=r.StartsAtUtc,EndsAtUtc=r.EndsAtUtc};db.Add(row);Touch(offering);History(db,who,http,id,null,"session.created",r.Name);await db.SaveChangesAsync(ct);return Results.Ok(row);}).RequireAuthorization("events.manage");
        api.MapPost("/offerings/{id:guid}/enrollments",async(Guid id,EnrollmentRequest r,EventsDbContext db,ICrmDirectory crm,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
        {
            var offering=await OfferingAsync(db,id,ct);Check(offering.Status=="published","The offering is cancelled.");var contact=await crm.FindActiveAsync(r.ContactId,ct)??throw new KeyNotFoundException();Check(contact.Kind=="contact","Choose a Contact.");
            var status=offering.Kind=="course"?"submitted":await HasSeat(db,id,offering.Capacity,ct)?"registered":"waitlisted";
            var row=new Enrollment{TenantId=who.TenantId!.Value,OfferingId=id,ContactId=contact.Id,ContactName=contact.Name,Status=status};db.Add(row);Touch(offering);History(db,who,http,id,row.Id,"enrollment."+status,"Created booking/application");await db.SaveChangesAsync(ct);return Results.Ok(row);
        }).RequireAuthorization("events.manage");
        api.MapPost("/enrollments/{id:guid}/{action}",async(Guid id,string action,EnrollmentChange r,EventsDbContext db,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
        {
            var row=await db.Set<Enrollment>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();if(row.Version!=r.Version)return Conflict();Choice(action,"approve","reject","promote","attend","cancel","transfer");var reason=Text(r.Reason,500);var source=await OfferingAsync(db,row.OfferingId,ct);Check(source.Status=="published","The offering is cancelled.");
            if(action is "approve" or "promote"){Check(action=="approve"?row.Status=="submitted":row.Status=="waitlisted","Invalid enrollment transition.");Check(await HasSeat(db,source.Id,source.Capacity,ct),"This offering is full.");row.Status="registered";}
            else if(action=="reject"){Check(row.Status=="submitted","Only submitted applications can be rejected.");row.Status="rejected";}
            else if(action=="attend"){Check(row.Status=="registered","Only registered participants can attend.");row.Status="attended";}
            else if(action=="cancel"){Check(row.Status is "registered" or "submitted" or "waitlisted","This enrollment cannot be cancelled.");row.Status="cancelled";}
            else
            {
                Check(row.Status is "registered" or "waitlisted","Only registered/waitlisted enrollments can transfer.");Check(r.TargetOfferingId!=null&&r.TargetOfferingId!=source.Id,"Choose a different destination.");var target=await OfferingAsync(db,r.TargetOfferingId!.Value,ct);Check(target.Status=="published"&&target.Kind==source.Kind,"Choose a published destination of the same kind.");Check(await HasSeat(db,target.Id,target.Capacity,ct),"The destination is full.");row.OfferingId=target.Id;row.Status="registered";Touch(target);History(db,who,http,target.Id,row.Id,"enrollment.transferred-in",reason);
            }
            Touch(source);History(db,who,http,source.Id,row.Id,"enrollment."+action,reason);await db.SaveChangesAsync(ct);return Results.Ok(row);
        }).RequireAuthorization("events.manage");
        api.MapPost("/enrollments/{id:guid}/result",async(Guid id,ResultRequest r,EventsDbContext db,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
        {
            var row=await db.Set<Enrollment>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();if(row.Version!=r.Version)return Conflict();var offering=await OfferingAsync(db,row.OfferingId,ct);Check(offering.Kind=="exam"&&row.Status=="attended","Results require an attended exam booking.");Check(r.Score is >=0 and <=100,"Score must be 0–100.");var result=new ExamResult{TenantId=who.TenantId!.Value,EnrollmentId=id,Score=r.Score,Outcome=r.Score>=offering.PassingScore?"pass":"fail",ActorUserId=who.UserId!.Value};db.Add(result);Touch(offering);History(db,who,http,offering.Id,id,"exam.result-recorded","Published result");await db.SaveChangesAsync(ct);return Results.Ok(result);
        }).RequireAuthorization("events.results");
        api.MapPost("/offerings/{id:guid}/cancel",async(Guid id,CancelRequest r,EventsDbContext db,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
        {var offering=await OfferingAsync(db,id,ct);if(offering.Version!=r.Version)return Conflict();Check(offering.Status=="published","Already cancelled.");var reason=Text(r.Reason,500);offering.Status="cancelled";foreach(var row in await db.Set<Enrollment>().Where(x=>x.OfferingId==id&&(x.Status=="registered"||x.Status=="waitlisted"||x.Status=="submitted")).ToListAsync(ct)){row.Status="cancelled";History(db,who,http,id,row.Id,"enrollment.cancelled",reason);}History(db,who,http,id,null,"offering.cancelled",reason);await db.SaveChangesAsync(ct);return Results.Ok(offering);}).RequireAuthorization("events.manage");
        return routes;
    }
    private static async Task<Offering> OfferingAsync(EventsDbContext db,Guid id,CancellationToken ct)=>await db.Set<Offering>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();
    private static async Task<bool> HasSeat(EventsDbContext db,Guid id,int capacity,CancellationToken ct)=>await db.Set<Enrollment>().CountAsync(x=>x.OfferingId==id&&(x.Status=="registered"||x.Status=="attended"),ct)<capacity;
    private static void Touch(Offering row){row.Version=Guid.NewGuid();row.UpdatedAtUtc=DateTime.UtcNow;}
    private static void History(EventsDbContext db,IRequestIdentity who,HttpContext http,Guid offering,Guid? enrollment,string action,string reason)=>db.Add(new EventHistory{TenantId=who.TenantId!.Value,OfferingId=offering,EnrollmentId=enrollment,Action=action,Reason=reason,ActorUserId=who.UserId!.Value,CorrelationId=http.TraceIdentifier});
}
public sealed record OfferingRequest(string Kind,string Name,string Venue,DateTime StartsAtUtc,DateTime EndsAtUtc,int Capacity,int PassingScore);
public sealed record SessionRequest(string Name,DateTime StartsAtUtc,DateTime EndsAtUtc);
public sealed record EnrollmentRequest(Guid ContactId);
public sealed record EnrollmentChange(Guid Version,string Reason,Guid? TargetOfferingId);
public sealed record ResultRequest(Guid Version,int Score);
public sealed record CancelRequest(Guid Version,string Reason);
