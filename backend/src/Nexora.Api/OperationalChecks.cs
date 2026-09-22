using Nexora.Modules.Content;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
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
  // Minimal API services are resolved from the HTTP request scope, not the root container.
  app.MapGet("/health/ready", (IServiceProvider services, CancellationToken ct) =>
  {
   DbContext[] databases =
   [
    services.GetRequiredService<ContentDbContext>(),
    services.GetRequiredService<NexoraIdentityDbContext>(),
    services.GetRequiredService<CrmDbContext>(),
    services.GetRequiredService<MembershipDbContext>(),
    services.GetRequiredService<EventsDbContext>(),
    services.GetRequiredService<FinanceDbContext>(),
    services.GetRequiredService<EngagementDbContext>(),
    services.GetRequiredService<WorkDbContext>()
   ];
   return CheckDatabasesAsync(databases, app.Logger, ct, TimeSpan.FromSeconds(30));
  });
 }

 internal static async Task<IResult> CheckDatabasesAsync(
  IEnumerable<DbContext> databases, ILogger logger, CancellationToken requestAborted, TimeSpan timeout)
 {
  using var budget = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
  budget.CancelAfter(timeout);
  foreach (var db in databases)
  {
   var timer = Stopwatch.StartNew();
   var context = db.GetType().Name;
   var connection = db.Database.GetDbConnection();
   var server = connection.DataSource;
   var database = connection.Database;
   var phase = "connection";
   var opened = false;
   try
   {
    logger.LogInformation("Readiness {DbContext} -> {DataSource} -> {Database}: connecting; connection timeout={ConnectionTimeoutSeconds}s command timeout={CommandTimeoutSeconds}s",
     context, server, database, connection.ConnectionTimeout,
     db.Database.GetCommandTimeout() ?? (connection is SqlConnection ? 30 : null));
    // Open explicitly: CanConnectAsync suppresses provider errors and adds an existence
    // probe. Keep this connection open for the context's migration-history query.
    await db.Database.OpenConnectionAsync(budget.Token);
    opened = true;
    logger.LogInformation("Readiness {DbContext} -> {DataSource} -> {Database}: connected in {ElapsedMs}ms",
     context, server, database, timer.ElapsedMilliseconds);
    phase = "migrations";
    // All SQL contexts currently share one catalog, but each owns a different history.
    // Checking only the first context would miss an unapplied module migration.
    if (db.Database.IsSqlServer())
    {
     var pending = (await db.Database.GetPendingMigrationsAsync(budget.Token)).ToArray();
     if (pending.Length != 0)
     {
      logger.LogWarning("Readiness {DbContext} -> {DataSource} -> {Database}: pending migrations {PendingMigrations}; elapsed={ElapsedMs}ms",
       context, server, database, string.Join(", ", pending), timer.ElapsedMilliseconds);
      return Results.StatusCode(503);
     }
    }
    logger.LogInformation("Readiness {DbContext} -> {DataSource} -> {Database}: ready in {ElapsedMs}ms",
     context, server, database, timer.ElapsedMilliseconds);
   }
   catch (OperationCanceledException) when (requestAborted.IsCancellationRequested)
   {
    logger.LogInformation("Readiness {DbContext} -> {DataSource} -> {Database}: caller cancelled during {Phase} after {ElapsedMs}ms",
     context, server, database, phase, timer.ElapsedMilliseconds);
    throw; // Preserve request cancellation; never report a cancelled probe as ready.
   }
   catch (OperationCanceledException) when (budget.IsCancellationRequested)
   {
    logger.LogWarning("Readiness {DbContext} -> {DataSource} -> {Database}: readiness budget expired during {Phase} after {ElapsedMs}ms (budget={BudgetSeconds}s)",
     context, server, database, phase, timer.ElapsedMilliseconds, timeout.TotalSeconds);
    return Results.StatusCode(503);
   }
   catch (Exception error)
   {
    // Do not log connection strings or arbitrary provider exception messages: either
    // can contain credentials. Provider type/code plus target and phase are sufficient.
    logger.LogError("Readiness {DbContext} -> {DataSource} -> {Database}: failed during {Phase}; error={ErrorType} SQL number={SqlErrorNumber}; elapsed={ElapsedMs}ms",
     context, server, database, phase, error.GetType().Name, (error as SqlException)?.Number, timer.ElapsedMilliseconds);
    return Results.StatusCode(503);
   }
   finally
   {
    if (opened) await db.Database.CloseConnectionAsync();
   }
  }
  return Results.Ok(new { status = "ready" });
 }
}
