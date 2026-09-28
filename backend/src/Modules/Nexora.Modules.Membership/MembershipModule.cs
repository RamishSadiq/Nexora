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
    private static (int Number, int Size, int Offset) Pagination(string? page, string? pageSize)
    {
        var number = 1;
        var size = 25;
        Check(page == null || int.TryParse(page, out number), "Page must be a whole number.");
        Check(pageSize == null || int.TryParse(pageSize, out size), "Page size must be a whole number.");
        Check(number >= 1 && size is >= 1 and <= 100, "Use a page of at least 1 and a page size of 1–100.");
        var offset = ((long)number - 1) * size;
        Check(offset <= int.MaxValue, "The requested page is too large.");
        return (number, size, (int)offset);
    }
    public static IServiceCollection AddNexoraMembership(this IServiceCollection services, IConfiguration config)
    { services.AddScoped<Nexora.BuildingBlocks.Security.ICrmContextProvider, Nexora.Modules.Membership.Infrastructure.CrmContextProvider>();
        services.AddScoped<Nexora.BuildingBlocks.Security.ICrmContextProvider, Nexora.Modules.Membership.Infrastructure.CrmEvidenceContext>();
        services.AddDbContext<MembershipDbContext>(o => o.UseSqlServer(config.GetConnectionString("Nexora"), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory","membership"))); services.AddScoped<IReportDataset>(sp=>new ReportDataset("members","Memberships","membership.read",sp.GetRequiredService<MembershipDbContext>().Set<MemberSubscription>().AsNoTracking().Select(x=>new ReportRow{Id=x.Id,Label=x.ContactName,Status=x.Status,CreatedAtUtc=x.CreatedAtUtc})));
        services.AddScoped<IMemberMembershipDirectory, MemberMembershipDirectory>();
        services.AddScoped<IMemberApplicationService, MemberApplicationService>();
        services.AddScoped<IPortalNotificationService, PortalNotificationService>();
        services.AddScoped<IApplicationEvidenceService, ApplicationEvidenceService>();
        services.AddScoped<Nexora.BuildingBlocks.Documents.IDocumentScanner, Nexora.BuildingBlocks.Documents.ClamDocumentScanner>();
        return services; }
    public static IEndpointRouteBuilder MapNexoraMembership(this IEndpointRouteBuilder routes)
    {
        var api = routes.Module("membership").WithAttributes<MembershipDbContext>("membership.manage", typeof(MembershipProduct), typeof(MembershipApplication), typeof(MemberSubscription));
        api.MapMembershipOperations();
        api.MapApplicationEvidence();
        api.MapPut("/products/{id:guid}/portal", async (Guid id, PortalProductSettings r, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var product = await db.Set<MembershipProduct>().SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new KeyNotFoundException();
            if (product.Version != r.Version) return Conflict();
            Check(!(r.Enabled || r.OrganisationEnabled) || !string.IsNullOrWhiteSpace(r.Terms), "Provide member-facing terms before enabling applications.");
            Check(r.Terms != null && r.Terms.Length <= 4000, "Terms must not exceed 4000 characters.");
            product.PortalApplicationsEnabled = r.Enabled; product.PortalOrganisationApplicationsEnabled = r.OrganisationEnabled; product.PortalTerms = r.Terms!.Trim();
            History(db, who, http, id, "product.portal-settings", "Updated portal availability and terms");
            await db.SaveChangesAsync(ct); return Results.Ok(product);
        }).RequireAuthorization("membership.manage");
        api.MapGet("/contacts", (string? search, ICrmDirectory crm, CancellationToken ct) => crm.SearchContactsAsync(search,ct));
        api.MapGet("/accounts", (string? search, ICrmDirectory crm, CancellationToken ct) => crm.SearchAccountsAsync(search,ct));
        api.MapGet("/account-contact", async (Guid accountId, Guid contactId, ICrmDirectory crm, CancellationToken ct) => Results.Ok(new { linked = await crm.HasCurrentAccountContactAsync(accountId, contactId, ct) }));
        api.MapGet("/products", async (MembershipDbContext db, CancellationToken ct) => await db.Set<MembershipProduct>().OrderBy(x => x.Name).ToListAsync(ct));
        api.MapGet("/portal-notifications", async (MembershipDbContext db, CancellationToken ct) =>
            await db.Set<PortalNotification>().AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(ct));
        api.MapPost("/portal-notifications/{id:guid}/deliver", async (Guid id, MembershipDbContext db, CancellationToken ct) =>
        {
            var row = await db.Set<PortalNotification>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            Check(row.DeliveryStatus == "queued", "Only queued notifications can be delivered.");
            row.DeliveryStatus = "delivered"; row.DeliveryAttempts++;
            await db.SaveChangesAsync(ct); return Results.Ok(new { row.Id, row.DeliveryStatus, row.DeliveryAttempts });
        }).RequireAuthorization("membership.manage");
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
        api.MapGet("/applications", async (MembershipDbContext db, string? search, string? status, string? page, string? pageSize, CancellationToken ct) =>
        {
            var (number, size, offset) = Pagination(page, pageSize);
            var q = db.Set<MembershipApplication>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search)) { var term = Text(search); q = q.Where(x => (x.ContactName.Contains(term) || (x.AccountName != null && x.AccountName.Contains(term)))); }
            if (!string.IsNullOrWhiteSpace(status)) { Choice(status,"submitted","approved","rejected"); q = q.Where(x => x.Status == status); }
            var totalCount = await q.CountAsync(ct);
            var items = await q.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id).Skip(offset).Take(size).ToListAsync(ct);
            return Results.Ok(new { items, totalCount, page = number, pageSize = size });
        });
        api.MapPost("/applications", async (ApplicationRequest r, MembershipDbContext db, ICrmDirectory crm, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var application = await MembershipApplicationCommand.CreateAsync(r, db, crm, who, http.TraceIdentifier, ct);
            await db.SaveChangesAsync(ct); return Results.Ok(application);
        }).RequireAuthorization("membership.manage");
        api.MapPost("/applications/{id:guid}/decision", async (Guid id, DecisionRequest r, MembershipDbContext db, ICrmDirectory crm, IPortalRecipientDirectory recipients, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var application = await db.Set<MembershipApplication>().SingleOrDefaultAsync(x => x.Id == id,ct) ?? throw new KeyNotFoundException();
            if (application.Version != r.Version) return Conflict(); Choice(r.Decision,"approve","reject"); Check(application.Status == "submitted","This application already has a decision.");
            application.DecisionReason = Text(r.Reason,500);
            if (r.Decision == "approve")
            {
                Check(await crm.FindActiveAsync(application.ContactId,ct) != null,"The Contact is no longer active.");
                if (application.AccountId.HasValue)
                {
                    Check(await crm.FindActiveAsync(application.AccountId.Value, ct) != null, "The organisation is no longer active.");
                    Check(await crm.HasCurrentAccountContactAsync(application.AccountId.Value, application.ContactId, ct), "The Contact is no longer linked to this organisation.");
                }
                var requestedEnd = application.RequestedEndUtc ?? application.RequestedStartUtc.AddMonths(application.TermMonths);
                Check(!await db.Set<MemberSubscription>().AnyAsync(x =>
                    (application.AccountId.HasValue ? x.AccountId == application.AccountId : x.AccountId == null && x.ContactId == application.ContactId)
                    && x.Status == "active",ct),
                    "This applicant already has an active membership. End it before activating another membership.");
                var member = new MemberSubscription { TenantId = who.TenantId!.Value, AccountId = application.AccountId, AccountName = application.AccountName, ContactId = application.ContactId, ContactName = application.ContactName, ProductId = application.ProductId, StartsAtUtc = application.RequestedStartUtc, EndsAtUtc = requestedEnd, Rate = application.QuotedRate, Currency = application.Currency, ApplicationId = application.Id };
                db.Add(member); application.MembershipId = member.Id; History(db,who,http,member.Id,"membership.activated",r.Reason);
            }
            application.Status = r.Decision == "approve" ? "approved" : "rejected"; History(db,who,http,id,"application." + application.Status,r.Reason);
            var recipient = await recipients.FindPortalUserIdAsync(who.TenantId!.Value, application.ContactId, ct);
            if (recipient is Guid userId) db.Add(new PortalNotification { TenantId = who.TenantId.Value, RecipientUserId = userId, ApplicationId = application.Id, Kind = "application." + application.Status, Subject = application.Status == "approved" ? "Application approved" : "Application rejected", Body = application.Status == "approved" ? "Your application was approved." : "Your application was rejected.", DeliveryStatus = "queued" });
            await db.SaveChangesAsync(ct); return Results.Ok(application);
        }).RequireAuthorization("membership.decide");
        api.MapGet("/members", async (MembershipDbContext db, string? search, string? status, string? page, string? pageSize, CancellationToken ct) =>
        {
            var (number, size, offset) = Pagination(page, pageSize);
            var q = db.Set<MemberSubscription>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search)) { var term = Text(search); q = q.Where(x => (x.ContactName.Contains(term) || (x.AccountName != null && x.AccountName.Contains(term)))); }
            if (!string.IsNullOrWhiteSpace(status)) { Choice(status,"active","cancelled","lapsed"); q = q.Where(x => x.Status == status); }
            var totalCount = await q.CountAsync(ct);
            var items = await q.OrderBy(x => x.ContactName).ThenBy(x => x.Id).Skip(offset).Take(size).ToListAsync(ct);
            return Results.Ok(new { items, totalCount, page = number, pageSize = size });
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
                Check(!await db.Set<MemberSubscription>().AnyAsync(x => x.Id != member.Id && x.Status == "active" &&
                    (member.AccountId.HasValue ? x.AccountId == member.AccountId : x.AccountId == null && x.ContactId == member.ContactId), ct),
                    "This applicant already has an active membership. End it before activating another membership.");
                var quote = await MembershipOperations.RenewalQuoteAsync(db, member, action == "reinstate", ct);
                member.EndsAtUtc = quote.EndsAtUtc; member.Rate = quote.Total; member.Currency = quote.Currency; member.Status = "active";
            }
            else { Check(member.Status == "active","Only active memberships can be cancelled or lapsed."); if (action == "lapse") Check(member.EndsAtUtc <= DateTime.UtcNow,"A membership cannot lapse before its term ends."); member.Status = action == "cancel" ? "cancelled" : "lapsed"; }
            History(db,who,http,id,"membership." + action,reason,previousEnd,member.EndsAtUtc); await db.SaveChangesAsync(ct); return Results.Ok(member);
        }).RequireAuthorization("membership.manage");
        api.MapGet("/history/{id:guid}", async (Guid id, MembershipDbContext db, CancellationToken ct) => await db.Set<MembershipHistory>().Where(x => x.SubjectId == id).OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(ct));
        return routes;
    }
    private static string? TextOptional(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : Text(value, max);
    private static void ValidateProduct(ProductRequest r) { Text(r.Name); Text(r.Category,80); Check(r.TermMonths is >= 1 and <= 60,"Terms must be between 1 and 60 months."); Check(r.Rate >= 0 && r.Rate < 100000000 && decimal.Round(r.Rate,2) == r.Rate,"Enter a nonnegative rate with two decimal places."); Choice(r.Currency,"GBP","EUR","USD"); }
    private static void History(MembershipDbContext db, IRequestIdentity who, HttpContext http, Guid id, string action, string reason, DateTime? previous = null, DateTime? next = null) => db.Add(new MembershipHistory { TenantId = who.TenantId!.Value, SubjectId = id, ActorUserId = who.UserId!.Value, Action = action, Reason = reason, CorrelationId = http.TraceIdentifier, PreviousEndUtc = previous, NewEndUtc = next });
}
public sealed record ProductRequest(string Name,string Category,int TermMonths,decimal Rate,string Currency,bool IsActive,Guid? Version) : IAttributeRequest { public Dictionary<Guid, AttributeInput>? Attributes { get; init; } }
public sealed record ApplicationRequest(Guid ContactId,Guid? AccountId,Guid ProductId,DateTime StartsAtUtc,string ApplicationType="corporate",string? ApplicantRole=null,bool TermsAccepted=false,string? ApplicantNotes=null) : IAttributeRequest { public Dictionary<Guid, AttributeInput>? Attributes { get; init; } }
public sealed record DecisionRequest(Guid Version,string Decision,string Reason);
public sealed record PortalProductSettings(Guid Version, bool Enabled, string Terms, bool OrganisationEnabled = false);
public sealed record ChangeRequest(Guid Version,string Reason);

