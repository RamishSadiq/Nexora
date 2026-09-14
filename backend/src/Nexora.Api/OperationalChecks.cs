using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using Nexora.Modules.Crm.Infrastructure;
using Nexora.Modules.Membership.Infrastructure;
using Nexora.Modules.Events.Infrastructure;
using Nexora.Modules.Finance.Infrastructure;
using Nexora.Modules.Engagement.Infrastructure;
using Nexora.Modules.Work.Infrastructure;
namespace Nexora.Api;
public static class OperationalChecks
{
 private static readonly Meter Meter=new("Nexora.Api","1.0");
 private static readonly Histogram<double> Duration=Meter.CreateHistogram<double>("nexora.http.duration","ms");
 public static void UseNexoraTelemetry(this WebApplication app)
 {
  app.Use(async(http,next)=>{var timer=Stopwatch.StartNew();http.Response.OnStarting(()=>{http.Response.Headers["X-Request-ID"]=http.TraceIdentifier;http.Response.Headers["X-Content-Type-Options"]="nosniff";return Task.CompletedTask;});try{await next(http);}finally{var route=(http.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText??"unmatched";Duration.Record(timer.Elapsed.TotalMilliseconds,new KeyValuePair<string,object?>("http.route",route),new KeyValuePair<string,object?>("http.response.status_code",http.Response.StatusCode));app.Logger.LogInformation("Request {Method} {Route} returned {Status} in {ElapsedMs} ms trace={Trace}",http.Request.Method,route,http.Response.StatusCode,timer.ElapsedMilliseconds,http.TraceIdentifier);}});
 }
 public static void MapNexoraReadiness(this WebApplication app)
 {
  app.MapGet("/health/ready",async(IServiceProvider services,CancellationToken ct)=>{DbContext[] databases=[services.GetRequiredService<NexoraIdentityDbContext>(),services.GetRequiredService<CrmDbContext>(),services.GetRequiredService<MembershipDbContext>(),services.GetRequiredService<EventsDbContext>(),services.GetRequiredService<FinanceDbContext>(),services.GetRequiredService<EngagementDbContext>(),services.GetRequiredService<WorkDbContext>()];try{foreach(var db in databases){if(!await db.Database.CanConnectAsync(ct))return Results.StatusCode(503);if(db.Database.IsSqlServer()&&(await db.Database.GetPendingMigrationsAsync(ct)).Any())return Results.StatusCode(503);}return Results.Ok(new{status="ready"});}catch(Exception e) when(e is not OperationCanceledException){return Results.StatusCode(503);}});
 }
}
