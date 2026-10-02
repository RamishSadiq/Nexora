using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexora.BuildingBlocks.Security;

namespace Nexora.Modules.Content;

public sealed class ContentScheduler(IServiceScopeFactory scopes, ILogger<ContentScheduler> log) : BackgroundService
{
    private sealed record Publisher(Guid? TenantId, Guid? UserId) : IRequestIdentity;
    public static async Task DispatchAsync(IServiceProvider services, DateTime now, CancellationToken ct)
    {
        var source = services.GetRequiredService<ContentDbContext>();
        var jobs = await source.Set<ScheduledPublication>().IgnoreQueryFilters().AsNoTracking().Where(j => j.Status == "pending" && j.PublishAtUtc <= now).OrderBy(j => j.PublishAtUtc).Take(50).ToListAsync(ct);
        foreach (var candidate in jobs)
        {
            var principal=await services.GetRequiredService<IWorkflowPrincipalDirectory>().ResolveAsync(candidate.TenantId,candidate.PublisherId,ct);
            using var eventIdentity=services.GetRequiredService<WorkflowExecutionContext>().Enter(principal??new(candidate.TenantId,candidate.PublisherId,[]));
            await using var db = new ContentDbContext(services.GetRequiredService<DbContextOptions<ContentDbContext>>(), new Publisher(candidate.TenantId, candidate.PublisherId));
            var job = await db.Set<ScheduledPublication>().SingleOrDefaultAsync(j => j.Id == candidate.Id && j.Status == "pending", ct);
            if (job == null) continue;
            var page = await db.Pages.SingleAsync(p => p.Id == job.PageId, ct);
            var revision = await db.Versions.SingleAsync(v => v.Id == job.RevisionId && v.PageId == page.Id, ct);
            var ids = ContentModule.MediaIds(revision).Distinct().ToArray();
            var validMedia = await db.Set<MediaAsset>().CountAsync(m => ids.Contains(m.Id) && m.SiteId == page.SiteId && m.IsPublic && m.ScanStatus == "clean", ct) == ids.Length;
            if (!await services.GetRequiredService<IScheduledPublisherDirectory>().CanPublishAsync(job.TenantId, job.PublisherId, ct) || !validMedia)
            { job.Status = "failed"; job.Failure = "Publisher access or public image grants are no longer valid."; }
            else
            {
                page.PublishedId = revision.Id; job.Status = "published";
                db.Add(new Publication { TenantId = job.TenantId, PageId = page.Id, PageVersionId = revision.Id, ActorId = job.PublisherId });
            }
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException) { /* A cancellation, manual publication or another worker won. Retry pending work next tick. */ }
        }
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try { using var scope = scopes.CreateScope(); await DispatchAsync(scope.ServiceProvider, DateTime.UtcNow, stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception e) { log.LogError(e, "Scheduled publication dispatch failed; pending jobs will retry."); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        { /* Normal host shutdown also cancels the idle timer wait. */ }
    }
}
