using Nexora.Api;
using System.Reflection;
using Nexora.Modules.Work;
using Nexora.Modules.Work.Infrastructure;
using Nexora.Modules.Engagement;
using Nexora.Modules.Engagement.Infrastructure;
using Nexora.Modules.Finance;
using Nexora.Modules.Finance.Infrastructure;
using Nexora.Modules.Events;
using Nexora.Modules.Events.Infrastructure;
using Nexora.Modules.Membership;
using Nexora.Modules.Membership.Infrastructure;
using Nexora.Modules.Crm;
using Nexora.Modules.Crm.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Nexora.BuildingBlocks.Platform;
using Nexora.Modules.Identity;
using Nexora.Modules.Identity.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddNexoraIdentity(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddNexoraCrm(builder.Configuration);
builder.Services.AddNexoraMembership(builder.Configuration);
builder.Services.AddNexoraEvents(builder.Configuration);
builder.Services.AddNexoraFinance(builder.Configuration);
builder.Services.AddNexoraWork(builder.Configuration);
builder.Services.AddNexoraEngagement(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("NexoraWeb", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Web:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseNexoraTelemetry();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("NexoraWeb");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString().ToLowerInvariant(),
            service = "nexora-api",
            checkedAt = DateTimeOffset.UtcNow,
        });
    },
});

app.MapGet("/api/v1/platform", () =>
{
    var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";
    return Results.Ok(new PlatformDescriptor(
        "Nexora",
        "nexora-api",
        version,
        ["platform-shell", "identity", "tenancy", "crm", "membership", "events", "finance", "engagement", "work", "insights"]));
})
.WithName("GetPlatformDescriptor")
.WithTags("Platform");

app.MapNexoraIdentityEndpoints();
app.MapNexoraCrm();
app.MapNexoraMembership();
app.MapNexoraEvents();
app.MapNexoraFinance();
app.MapNexoraWork();
app.MapNexoraEngagement();
app.MapNexoraReporting();
app.MapNexoraReadiness();

if (app.Environment.IsDevelopment())
{
    await IdentitySeeder.SeedDevelopmentIdentityAsync(app.Services, builder.Configuration);
    if (!string.IsNullOrWhiteSpace(builder.Configuration["Identity:SeedAdminPassword"]))
    {
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CrmDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<MembershipDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<EventsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<FinanceDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<WorkDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<EngagementDbContext>().Database.MigrateAsync();
    }
}

app.Run();

public partial class Program;
