using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Reporting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Endpoints;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
namespace Nexora.Modules.Membership;
public static class MembershipModule
{
    public static IServiceCollection AddNexoraMembership(this IServiceCollection services, IConfiguration config)
    { services.AddDbContext<MembershipDbContext>(o => o.UseSqlServer(config.GetConnectionString("Nexora"), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory","membership"))); services.AddScoped<IReportDataset>(sp=>new ReportDataset("members","Memberships","membership.read",sp.GetRequiredService<MembershipDbContext>().Set<MemberSubscription>().AsNoTracking().Select(x=>new ReportRow{Id=x.Id,Label=x.ContactName,Status=x.Status,CreatedAtUtc=x.CreatedAtUtc})));
        return services; }
    public static IEndpointRouteBuilder MapNexoraMembership(this IEndpointRouteBuilder routes)
    {
        var api = routes.Module("membership").WithAttributes<MembershipDbContext>("membership.manage", typeof(MembershipProduct), typeof(MembershipApplication), typeof(MemberSubscription));
        api.MapGet("/contacts", (string? search, ICrmDirectory crm, CancellationToken ct) => crm.SearchContactsAsync(search,ct));
        api.MapGet("/products", async (MembershipDbContext db, CancellationToken ct) => await db.Set<MembershipProduct>().OrderBy(x => x.Name).ToListAsync(ct));
        api.MapPost("/products", async (ProductRequest r, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            ValidateProduct(r);
            var product = new MembershipProduct { TenantId = who.TenantId!.Value, Name = Text(r.Name), Category = Text(r.Category,80), TermMonths = r.TermMonths, Rate = r.Rate, Currency = r.Currency, IsActive = r.IsActive };
            db.Add(product); History(db,who,http,product.Id,"product.created","Created product"); await db.SaveChangesAsync(ct); return Results.Ok(product);
        }).RequireAuthorization("membership.manage");
        api.MapPatch("/products/{id:guid}", async (Guid id, ProductRequest r, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var product = await db.Set<MembershipProduct>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            if (r.Version != product.Version) return Conflict(); ValidateProduct(r);
            product.Name = Text(r.Name); product.Category = Text(r.Category,80); product.TermMonths = r.TermMonths; product.Rate = r.Rate; product.Currency = r.Currency; product.IsActive = r.IsActive;
            History(db,who,http,id,"product.updated","Updated catalogue; existing quotes unchanged"); await db.SaveChangesAsync(ct); return Results.Ok(product);
        }).RequireAuthorization("membership.manage");
        api.MapGet("/applications", async (MembershipDbContext db, CancellationToken ct) => await db.Set<MembershipApplication>().OrderByDescending(x => x.CreatedAtUtc).Take(200).ToListAsync(ct));
        api.MapPost("/applications", async (ApplicationRequest r, MembershipDbContext db, ICrmDirectory crm, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var contact = await crm.FindActiveAsync(r.ContactId,ct) ?? throw new KeyNotFoundException(); Check(contact.Kind == "contact", "Membership requires a Contact.");
            var product = await db.Set<MembershipProduct>().SingleOrDefaultAsync(x => x.Id == r.ProductId && x.IsActive,ct) ?? throw new KeyNotFoundException();
            Check(r.StartsAtUtc.Date >= DateTime.UtcNow.Date && r.StartsAtUtc.Date <= DateTime.UtcNow.Date.AddYears(2), "Choose a start date within the next two years.");
            var application = new MembershipApplication { TenantId = who.TenantId!.Value, ContactId = contact.Id, ContactName = contact.Name, ProductId = product.Id, RequestedStartUtc = DateTime.SpecifyKind(r.StartsAtUtc.Date, DateTimeKind.Utc), QuotedRate = product.Rate, Currency = product.Currency, TermMonths = product.TermMonths };
            db.Add(application); History(db,who,http,application.Id,"application.submitted","Submitted for review"); await db.SaveChangesAsync(ct); return Results.Ok(application);
        }).RequireAuthorization("membership.manage");
        api.MapPost("/applications/{id:guid}/decision", async (Guid id, DecisionRequest r, MembershipDbContext db, ICrmDirectory crm, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var application = await db.Set<MembershipApplication>().SingleOrDefaultAsync(x => x.Id == id,ct) ?? throw new KeyNotFoundException();
            if (application.Version != r.Version) return Conflict(); Choice(r.Decision,"approve","reject"); Check(application.Status == "submitted","This application already has a decision.");
            application.DecisionReason = Text(r.Reason,500);
            if (r.Decision == "approve")
            {
                Check(await crm.FindActiveAsync(application.ContactId,ct) != null,"The Contact is no longer active.");
                Check(!await db.Set<MemberSubscription>().AnyAsync(x => x.ContactId == application.ContactId && x.ProductId == application.ProductId && x.Status == "active" && x.EndsAtUtc > application.RequestedStartUtc,ct),"This Contact already has an overlapping active membership.");
                var member = new MemberSubscription { TenantId = who.TenantId!.Value, ContactId = application.ContactId, ContactName = application.ContactName, ProductId = application.ProductId, StartsAtUtc = application.RequestedStartUtc, EndsAtUtc = application.RequestedStartUtc.AddMonths(application.TermMonths), Rate = application.QuotedRate, Currency = application.Currency, ApplicationId = application.Id };
                db.Add(member); application.MembershipId = member.Id; History(db,who,http,member.Id,"membership.activated",r.Reason);
            }
            application.Status = r.Decision == "approve" ? "approved" : "rejected"; History(db,who,http,id,"application." + application.Status,r.Reason); await db.SaveChangesAsync(ct); return Results.Ok(application);
        }).RequireAuthorization("membership.decide");
        api.MapGet("/members", async (MembershipDbContext db, string? search, string? status, CancellationToken ct) =>
        {
            var q = db.Set<MemberSubscription>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search)) { var term = Text(search); q = q.Where(x => x.ContactName.Contains(term)); }
            if (!string.IsNullOrWhiteSpace(status)) { Choice(status,"active","cancelled","lapsed"); q = q.Where(x => x.Status == status); }
            return Results.Ok(await q.OrderBy(x => x.ContactName).ThenBy(x => x.Id).Take(200).ToListAsync(ct));
        });
        api.MapGet("/members/{id:guid}", async (Guid id, MembershipDbContext db, CancellationToken ct) =>
        {
            var member = await db.Set<MemberSubscription>().SingleOrDefaultAsync(x => x.Id == id,ct) ?? throw new KeyNotFoundException();
            return Results.Ok(new { member, history = await db.Set<MembershipHistory>().Where(x => x.SubjectId == id).OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(ct) });
        });
        api.MapPost("/members/{id:guid}/{action}", async (Guid id, string action, ChangeRequest r, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var member = await db.Set<MemberSubscription>().SingleOrDefaultAsync(x => x.Id == id,ct) ?? throw new KeyNotFoundException();
            if (r.Version != member.Version) return Conflict(); Choice(action,"renew","cancel","lapse","reinstate"); var reason = Text(r.Reason,500);
            var previousEnd = member.EndsAtUtc;
            if (action is "renew" or "reinstate")
            {
                Check(action == "renew" ? member.Status is "active" or "lapsed" : member.Status == "cancelled", "This transition is not allowed from the current status.");
                var product = await db.Set<MembershipProduct>().SingleAsync(x => x.Id == member.ProductId,ct); Check(product.IsActive,"This membership product is inactive.");
                var start = member.EndsAtUtc > DateTime.UtcNow.Date && action == "renew" ? member.EndsAtUtc : DateTime.UtcNow.Date;
                member.EndsAtUtc = start.AddMonths(product.TermMonths); member.Rate = product.Rate; member.Currency = product.Currency; member.Status = "active";
            }
            else { Check(member.Status == "active","Only active memberships can be cancelled or lapsed."); if (action == "lapse") Check(member.EndsAtUtc <= DateTime.UtcNow,"A membership cannot lapse before its term ends."); member.Status = action == "cancel" ? "cancelled" : "lapsed"; }
            History(db,who,http,id,"membership." + action,reason,previousEnd,member.EndsAtUtc); await db.SaveChangesAsync(ct); return Results.Ok(member);
        }).RequireAuthorization("membership.manage");
        api.MapGet("/history/{id:guid}", async (Guid id, MembershipDbContext db, CancellationToken ct) => await db.Set<MembershipHistory>().Where(x => x.SubjectId == id).OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(ct));
        return routes;
    }
    private static void ValidateProduct(ProductRequest r) { Text(r.Name); Text(r.Category,80); Check(r.TermMonths is >= 1 and <= 60,"Terms must be between 1 and 60 months."); Check(r.Rate >= 0 && r.Rate < 100000000 && decimal.Round(r.Rate,2) == r.Rate,"Enter a nonnegative rate with two decimal places."); Choice(r.Currency,"GBP","EUR","USD"); }
    private static void History(MembershipDbContext db, IRequestIdentity who, HttpContext http, Guid id, string action, string reason, DateTime? previous = null, DateTime? next = null) => db.Add(new MembershipHistory { TenantId = who.TenantId!.Value, SubjectId = id, ActorUserId = who.UserId!.Value, Action = action, Reason = reason, CorrelationId = http.TraceIdentifier, PreviousEndUtc = previous, NewEndUtc = next });
}
public sealed record ProductRequest(string Name,string Category,int TermMonths,decimal Rate,string Currency,bool IsActive,Guid? Version) : IAttributeRequest { public Dictionary<Guid, AttributeInput>? Attributes { get; init; } }
public sealed record ApplicationRequest(Guid ContactId,Guid ProductId,DateTime StartsAtUtc) : IAttributeRequest { public Dictionary<Guid, AttributeInput>? Attributes { get; init; } }
public sealed record DecisionRequest(Guid Version,string Decision,string Reason);
public sealed record ChangeRequest(Guid Version,string Reason);
