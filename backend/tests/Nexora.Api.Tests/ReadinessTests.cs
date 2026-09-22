using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Content;
using Nexora.Modules.Membership.Infrastructure;

namespace Nexora.Api.Tests;

public sealed class ReadinessTests
{
    [SqlServerFact]
    public async Task Shared_catalog_still_requires_each_modules_migrations()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER"))
        {
            InitialCatalog = "NexoraReadinessTest_" + Guid.NewGuid().ToString("N")
        };
        var identity = new TestIdentity();
        await using var content = new ContentDbContext(new DbContextOptionsBuilder<ContentDbContext>()
            .UseSqlServer(connection.ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "content")).Options, identity);
        await using var membership = new MembershipDbContext(new DbContextOptionsBuilder<MembershipDbContext>()
            .UseSqlServer(connection.ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "membership")).Options, identity);
        var logger = new CaptureLogger();
        try
        {
            await content.Database.MigrateAsync();
            var pending = await OperationalChecks.CheckDatabasesAsync([content, membership], logger, CancellationToken.None, TimeSpan.FromSeconds(30));
            Assert.Equal(503, Assert.IsAssignableFrom<IStatusCodeHttpResult>(pending).StatusCode);
            Assert.Contains(logger.Messages, x => x.Contains("MembershipDbContext") && x.Contains("pending migrations"));

            await membership.Database.MigrateAsync();
            var ready = await OperationalChecks.CheckDatabasesAsync([content, membership], logger, CancellationToken.None, TimeSpan.FromSeconds(30));
            Assert.Equal(200, Assert.IsAssignableFrom<IStatusCodeHttpResult>(ready).StatusCode);
        }
        finally
        {
            await content.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task Checks_each_context_and_closes_connections()
    {
        var logger = new CaptureLogger();
        var firstOpen = new ProbeInterceptor();
        var secondOpen = new ProbeInterceptor();
        await using var first = Context(firstOpen);
        await using var second = Context(secondOpen);

        var result = await OperationalChecks.CheckDatabasesAsync([first, second], logger, CancellationToken.None, TimeSpan.FromSeconds(5));

        Assert.Equal(200, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        Assert.Equal(1, firstOpen.Calls);
        Assert.Equal(1, secondOpen.Calls);
        Assert.Equal(ConnectionState.Closed, first.Database.GetDbConnection().State);
        Assert.Equal(ConnectionState.Closed, second.Database.GetDbConnection().State);
        Assert.Equal(2, logger.Messages.Count(x => x.Contains("ready in")));
    }

    [Fact]
    public async Task Logs_failed_context_and_target_without_exception_secrets()
    {
        var logger = new CaptureLogger();
        await using var first = Context(new ProbeInterceptor());
        await using var failure = new FailingContext(new DbContextOptionsBuilder<FailingContext>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(new ProbeInterceptor(error: new InvalidOperationException("Password=do-not-log"))).Options);
        var unvisited = new ProbeInterceptor();
        await using var last = Context(unvisited);

        var result = await OperationalChecks.CheckDatabasesAsync([first, failure, last], logger, CancellationToken.None, TimeSpan.FromSeconds(5));

        Assert.Equal(503, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        Assert.Equal(0, unvisited.Calls);
        Assert.Contains(logger.Messages, x => x.Contains("FailingContext") && x.Contains("failed during connection") && x.Contains("InvalidOperationException"));
        Assert.DoesNotContain(logger.Messages, x => x.Contains("do-not-log"));
    }

    [Fact]
    public async Task Caller_cancellation_is_logged_and_propagated()
    {
        var logger = new CaptureLogger();
        var blocked = new ProbeInterceptor(block: true);
        await using var db = Context(blocked);
        using var request = new CancellationTokenSource();
        var probe = OperationalChecks.CheckDatabasesAsync([db], logger, request.Token, TimeSpan.FromSeconds(5));
        await blocked.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        request.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => probe);
        Assert.Contains(logger.Messages, x => x.Contains("caller cancelled during connection"));
        Assert.DoesNotContain(logger.Messages, x => x.Contains("readiness budget expired"));
    }

    [Fact]
    public async Task Server_budget_returns_unavailable_without_cancelling_caller()
    {
        var logger = new CaptureLogger();
        await using var db = Context(new ProbeInterceptor(block: true));
        using var request = new CancellationTokenSource();

        var result = await OperationalChecks.CheckDatabasesAsync([db], logger, request.Token, TimeSpan.FromMilliseconds(100));

        Assert.Equal(503, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        Assert.False(request.IsCancellationRequested);
        Assert.Contains(logger.Messages, x => x.Contains("readiness budget expired during connection"));
    }

    [Fact]
    public async Task Provider_cancellation_without_cancelled_tokens_is_a_failure()
    {
        var logger = new CaptureLogger();
        await using var db = Context(new ProbeInterceptor(error: new TaskCanceledException()));
        var result = await OperationalChecks.CheckDatabasesAsync([db], logger, CancellationToken.None, TimeSpan.FromSeconds(5));

        Assert.Equal(503, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        Assert.Contains(logger.Messages, x => x.Contains("failed during connection") && x.Contains("TaskCanceledException"));
    }

    private static DbContext Context(ProbeInterceptor interceptor) => new(new DbContextOptionsBuilder()
        .UseSqlite("Data Source=:memory:").AddInterceptors(interceptor).Options);

    private sealed class FailingContext(DbContextOptions<FailingContext> options) : DbContext(options);
    private sealed record TestIdentity : IRequestIdentity
    {
        public Guid? TenantId => null;
        public Guid? UserId => null;
    }

    private sealed class ProbeInterceptor(bool block = false, Exception? error = null) : DbConnectionInterceptor
    {
        public int Calls { get; private set; }
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            Calls++;
            Entered.TrySetResult();
            if (error is not null) throw error;
            if (block) await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return result;
        }
    }

    private sealed class CaptureLogger : ILogger
    {
        public List<string> Messages { get; } = [];
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}
