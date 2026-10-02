using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.Modules.Content;

namespace Nexora.Api.Tests;

public sealed class ContentSchedulerLifecycleTests
{
    [Fact]
    public async Task Canceling_an_entered_idle_wait_completes_successfully()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        using var scheduler = new ContentScheduler(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<ContentScheduler>.Instance);
        using var stopping = new CancellationTokenSource();
        // Invoke the loop directly so cancellation cannot race the host's task scheduling.
        var execute = typeof(ContentScheduler).GetMethod("ExecuteAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var running = (Task)execute.Invoke(scheduler, [stopping.Token])!;
        Assert.False(running.IsCompleted);
        stopping.Cancel();
        await running.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(running.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Stopping_immediately_completes_without_faulting()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        using var scheduler = new ContentScheduler(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<ContentScheduler>.Instance);
        await scheduler.StartAsync(CancellationToken.None);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await scheduler.StopAsync(timeout.Token);
        Assert.NotNull(scheduler.ExecuteTask);
        // BackgroundService may cancel its queued execution before ExecuteAsync starts.
        // StopAsync must still complete normally; a canceled task is not a worker fault.
        Assert.True(scheduler.ExecuteTask.IsCompleted);
        Assert.False(scheduler.ExecuteTask.IsFaulted);
    }
}
