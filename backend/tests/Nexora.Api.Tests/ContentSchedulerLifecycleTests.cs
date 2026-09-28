using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.Modules.Content;

namespace Nexora.Api.Tests;

public sealed class ContentSchedulerLifecycleTests
{
    [Fact]
    public async Task Stopping_during_idle_timer_wait_completes_without_cancellation_escaping()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        using var scheduler = new ContentScheduler(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<ContentScheduler>.Instance);
        await scheduler.StartAsync(CancellationToken.None);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await scheduler.StopAsync(timeout.Token);
        Assert.NotNull(scheduler.ExecuteTask);
        await scheduler.ExecuteTask.WaitAsync(timeout.Token);
        Assert.True(scheduler.ExecuteTask.IsCompletedSuccessfully);
    }
}
