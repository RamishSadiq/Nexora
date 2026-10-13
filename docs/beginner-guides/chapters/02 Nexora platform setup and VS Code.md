# Nexora platform setup and VS Code

Document 2 of the Nexora learning guides  
Source snapshot 13 October 2026 at 17:12 UTC


Open the right project, recognise its folders, and run CRM, Portal and CMS locally.

### Recognise the screen and its scope

Open `CRM on 3017`, `Portal on 3018`, `CMS on 3019`, `API on 5097`. CRM routes use port 3017; member routes use 3018; public website routes can use 3019. Brackets name a route parameter, not text to paste into a browser. Choose a real record or site id from the application.

This guide covers Repository boundaries; Frontend and backend languages; Three debug profiles; Shared API startup; Environment variables and local database.

![File journey for Nexora platform setup and VS Code](../assets/platform/guide-02-map.png)

The diagram shows the file roles. The full paths below are what you paste into Ctrl+P.

### Find the files in VS Code

| Role | File and source ID | What it does |
| --- | --- | --- |
| Startup | S0004 next.config.ts | Local service setup |
| Server | S0575 Program.cs | Endpoints or server rules |
| Projects | S0185 Nexora.slnx | Backend solution projects |

**Startup S0004**  
[apps/web/next.config.ts](<D:/Projects new/Nexora/apps/web/next.config.ts>)

**Server S0575**  
[backend/src/Nexora.Api/Program.cs](<D:/Projects new/Nexora/backend/src/Nexora.Api/Program.cs>)

**Projects S0185**  
[backend/Nexora.slnx](<D:/Projects new/Nexora/backend/Nexora.slnx>)

The complete HTML manual opens these source IDs inside its Source library. The Word manual shows short labelled extracts so you can learn the flow before reading large files. Each individual Markdown guide includes its complete primary files after the walkthrough.

### Follow the request and the records

1. VS Code opens the Nexora repository folder.
2. A CRM, Portal or CMS debug profile runs its preparation task.
3. Prepare-LocalDebug.ps1 builds or reuses the shared API and waits for readiness.
4. Next.js starts with its own port and build output folder.
5. The frontend forwards /api requests to the configured API.

### Read the code with an explanation

Nexora uses one Next.js web project and a C# backend made of modules. The three local frontend processes share source but have separate debug outputs. A .tsx file draws a screen, .css changes appearance, .cs runs server rules, and .json supplies configuration. Company Dash appears in the launch menu but belongs to a separate repository; its source is outside this manual.

Source reference S0004 apps/web/next.config.ts

Selected opening lines with their original numbers. The complete file is in the HTML source library.

```ts
0001  import type { NextConfig } from "next";
0002  import path from "node:path";
0004  const nextConfig: NextConfig = {
0005    turbopack: { root: path.resolve(__dirname, "../..") },
0006    distDir: process.env.NEXORA_NEXT_DIST_DIR ?? ".next",
0007    poweredByHeader: false,
```


The import lines lead to shared components and helpers. Start at the exported component or route function; follow the request helper to the server rather than assuming the whole feature lives in this file.

Source reference S0575 backend/src/Nexora.Api/Program.cs

Extract from source line 83. A fragment from a long line is shown without the rest of that line.

```cs
0083  app.MapGet("/api/v1/platform", () =>
```


On the server, look for MapGet to load records, MapPost to create or perform an action, MapPut or MapPatch to change data, and RequireAuthorization for permissions. Some shared methods register endpoints indirectly. Find SaveChangesAsync to see the database write. A request method alone does not tell you every side effect.

Source reference S0185 backend/Nexora.slnx

Selected opening lines with their original numbers. The complete file is in the HTML source library.

```slnx
0001  <Solution>
0002    <Folder Name="/src/">
0003      <Project Path="src/Nexora.Api/Nexora.Api.csproj" />
0004      <Project Path="src/Nexora.Worker/Nexora.Worker.csproj" />
0005    </Folder>
0006    <Folder Name="/src/BuildingBlocks/">
```


Follow model fields to their request names and database mapping. An id points to a particular record. A tenant id limits the organisation. A version or concurrency stamp protects newer edits. Some model files describe contracts rather than tables; check the owning DbContext before assuming storage.

### Make or trace your first change

1. Press Ctrl+P and open [apps/web/next.config.ts](<D:/Projects new/Nexora/apps/web/next.config.ts>).
2. Press Ctrl+F and search for `NEXORA_API_URL`. In this snapshot it appears at line 11.
3. Read-only exercise: find the environment variable and its fallback. Compare it with the CRM launch profile. Write down why a terminal started without that variable can point to a different API; do not change a port.
4. Record the file, request path and server handler you found. This exercise is read-only; no stored records need to change.

Extract from source line 11. A fragment from a long line is shown without the rest of that line.

```ts
0011      const apiOrigin = process.env.NEXORA_API_URL ?? "http://localhost:5080";
```


These exercises have not been applied to the application. Their search phrases are verified against the captured source. Line numbers can move; the phrase and file path are your main navigation aids.

### Check a change and diagnose problems

Confirm the original screen still loads, the changed words appear in the intended place, and another screen using the same component still behaves correctly. If an action fails, inspect the browser Network response, the relevant helper and server validation message. Use the common error table at the beginning of the manual. Reload on a version conflict instead of removing the version check.

A syntax error after a text edit usually means a quote, brace or closing tag changed. Compare your diff with the original. An unchanged browser result can mean the wrong port, a saved published configuration overriding a default, or a running API that has not reloaded.

### Understand the current limits

The saved runtime path and SQL Express instance are workstation-specific. F5 dispatch itself is a manual action. Preparation can apply Development database migrations; it is not a production deployment procedure.

### Continue learning

Related guides [01 Nexora login page](#guide_01), [30 Nexora backend database and maintenance](#guide_30).

## Complete primary source files

These are the complete captured files, with numbered source lines. Other supporting files are included in the complete HTML manual. Configured local passwords, if any, are explicitly omitted. Do not paste the display line numbers into code.

### S0004 next.config.ts

[apps/web/next.config.ts](<D:/Projects new/Nexora/apps/web/next.config.ts>)

SHA256 `074cd2ab26cf11ba363d3977df22c2ef390fb45b774bb9e5baede47883bbd438`; 34 source lines; complete captured text.

```ts
0001  import type { NextConfig } from "next";
0002  import path from "node:path";
0003  
0004  const nextConfig: NextConfig = {
0005    turbopack: { root: path.resolve(__dirname, "../..") },
0006    distDir: process.env.NEXORA_NEXT_DIST_DIR ?? ".next",
0007    poweredByHeader: false,
0008    reactStrictMode: true,
0009    typedRoutes: true,
0010    async rewrites() {
0011      const apiOrigin = process.env.NEXORA_API_URL ?? "http://localhost:5080";
0012  
0013      return [
0014        {
0015          source: "/api/:path*",
0016          destination: `${apiOrigin}/api/:path*`,
0017        },
0018      ];
0019    },
0020    async headers() {
0021      return [
0022        {
0023          source: "/(.*)",
0024          headers: [
0025            { key: "X-Content-Type-Options", value: "nosniff" },
0026            { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
0027            { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=()" },
0028          ],
0029        },
0030      ];
0031    },
0032  };
0033  
0034  export default nextConfig;
```

### S0575 Program.cs

[backend/src/Nexora.Api/Program.cs](<D:/Projects new/Nexora/backend/src/Nexora.Api/Program.cs>)

SHA256 `ced3e2c3a7318361abebe285299108af8749da7016151db590405b9eac3d15ac`; 126 source lines; complete captured text.

```cs
0001  using Nexora.Modules.Workflow;
0002  using Nexora.Modules.Content;
0003  using Nexora.Api;
0004  using System.Reflection;
0005  using Nexora.Modules.Work;
0006  using Nexora.Modules.Work.Infrastructure;
0007  using Nexora.Modules.Engagement;
0008  using Nexora.Modules.Engagement.Infrastructure;
0009  using Nexora.Modules.Finance;
0010  using Nexora.Modules.Finance.Infrastructure;
0011  using Nexora.Modules.Events;
0012  using Nexora.Modules.Events.Infrastructure;
0013  using Nexora.Modules.Membership;
0014  using Nexora.Modules.Membership.Infrastructure;
0015  using Nexora.Modules.Crm;
0016  using Nexora.Modules.Crm.Infrastructure;
0017  using Microsoft.EntityFrameworkCore;
0018  using Microsoft.AspNetCore.Diagnostics.HealthChecks;
0019  using Nexora.BuildingBlocks.Platform;
0020  using Nexora.Modules.Identity;
0021  using Nexora.Modules.Identity.Infrastructure.Persistence;
0022  
0023  var builder = WebApplication.CreateBuilder(args);
0024  
0025  builder.Services.AddProblemDetails();
0026  builder.Services.AddOpenApi();
0027  builder.Services.AddHealthChecks();
0028  builder.Services.AddNexoraIdentity(builder.Configuration, builder.Environment.IsDevelopment());
0029  builder.Services.AddNexoraCrm(builder.Configuration);
0030  builder.Services.AddNexoraContent(builder.Configuration);
0031  builder.Services.AddNexoraMembership(builder.Configuration);
0032  builder.Services.AddNexoraEvents(builder.Configuration);
0033  builder.Services.AddNexoraFinance(builder.Configuration);
0034  builder.Services.AddNexoraWork(builder.Configuration);
0035  builder.Services.AddNexoraEngagement(builder.Configuration);
0036  builder.Services.AddNexoraWorkflow(builder.Configuration);
0037  builder.Services.AddCors(options =>
0038  {
0039      options.AddPolicy("NexoraWeb", policy =>
0040      {
0041          policy
0042              .WithOrigins(builder.Configuration.GetSection("Web:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"])
0043              .AllowAnyHeader()
0044              .AllowAnyMethod()
0045              .AllowCredentials();
0046      });
0047  });
0048  
0049  var app = builder.Build();
0050  
0051  app.UseNexoraTelemetry();
0052  app.UseExceptionHandler(new ExceptionHandlerOptions
0053  {
0054      StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
0055          ? badRequest.StatusCode
0056          : StatusCodes.Status500InternalServerError
0057  });
0058  app.UseHttpsRedirection();
0059  app.UseCors("NexoraWeb");
0060  app.UseRateLimiter();
0061  app.UseAuthentication();
0062  app.UseAuthorization();
0063  
0064  if (app.Environment.IsDevelopment())
0065  {
0066      app.MapOpenApi();
0067  }
0068  
0069  app.MapHealthChecks("/health", new HealthCheckOptions
0070  {
0071      ResponseWriter = async (context, report) =>
0072      {
0073          context.Response.ContentType = "application/json";
0074          await context.Response.WriteAsJsonAsync(new
0075          {
0076              status = report.Status.ToString().ToLowerInvariant(),
0077              service = "nexora-api",
0078              checkedAt = DateTimeOffset.UtcNow,
0079          });
0080      },
0081  });
0082  
0083  app.MapGet("/api/v1/platform", () =>
0084  {
0085      var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";
0086      return Results.Ok(new PlatformDescriptor(
0087          "Nexora",
0088          "nexora-api",
0089          version,
0090          ["platform-shell", "identity", "tenancy", "crm", "membership", "events", "finance", "engagement", "work", "insights"]));
0091  })
0092  .WithName("GetPlatformDescriptor")
0093  .WithTags("Platform");
0094  
0095  app.MapNexoraIdentityEndpoints();
0096  app.MapNexoraCrm();
0097  app.MapNexoraContent();
0098  app.MapNexoraMembership();
0099  app.MapNexoraEvents();
0100  app.MapNexoraFinance();
0101  app.MapNexoraWork();
0102  app.MapNexoraEngagement();
0103  app.MapNexoraWorkflow();
0104  app.MapNexoraReporting();
0105  app.MapNexoraReadiness();
0106  
0107  if (app.Environment.IsDevelopment())
0108  {
0109      await IdentitySeeder.SeedDevelopmentIdentityAsync(app.Services, builder.Configuration);
0110      if (!string.IsNullOrWhiteSpace(builder.Configuration["Identity:SeedAdminPassword"]))
0111      {
0112          await using var scope = app.Services.CreateAsyncScope();
0113          await scope.ServiceProvider.GetRequiredService<CrmDbContext>().Database.MigrateAsync();
0114          await scope.ServiceProvider.GetRequiredService<ContentDbContext>().Database.MigrateAsync();
0115          await scope.ServiceProvider.GetRequiredService<MembershipDbContext>().Database.MigrateAsync();
0116          await scope.ServiceProvider.GetRequiredService<EventsDbContext>().Database.MigrateAsync();
0117          await scope.ServiceProvider.GetRequiredService<FinanceDbContext>().Database.MigrateAsync();
0118          await scope.ServiceProvider.GetRequiredService<WorkDbContext>().Database.MigrateAsync();
0119          await scope.ServiceProvider.GetRequiredService<EngagementDbContext>().Database.MigrateAsync();
0120          await scope.ServiceProvider.GetRequiredService<WorkflowDbContext>().Database.MigrateAsync();
0121      }
0122  }
0123  
0124  app.Run();
0125  
0126  public partial class Program;
```

### S0185 Nexora.slnx

[backend/Nexora.slnx](<D:/Projects new/Nexora/backend/Nexora.slnx>)

SHA256 `733bec900c58e8293ca7103ca36c62e490982ed496b0eba33f4c13bc0c7bf564`; 24 source lines; complete captured text.

```slnx
0001  <Solution>
0002    <Folder Name="/src/">
0003      <Project Path="src/Nexora.Api/Nexora.Api.csproj" />
0004      <Project Path="src/Nexora.Worker/Nexora.Worker.csproj" />
0005    </Folder>
0006    <Folder Name="/src/BuildingBlocks/">
0007      <Project Path="src/BuildingBlocks/Nexora.BuildingBlocks/Nexora.BuildingBlocks.csproj" />
0008    </Folder>
0009    <Folder Name="/src/Modules/">
0010      <Project Path="src/Modules/Nexora.Modules.Workflow/Nexora.Modules.Workflow.csproj" />
0011      <Project Path="src/Modules/Nexora.Modules.Content/Nexora.Modules.Content.csproj" />
0012      <Project Path="src/Modules/Nexora.Modules.Work/Nexora.Modules.Work.csproj" />
0013      <Project Path="src/Modules/Nexora.Modules.Engagement/Nexora.Modules.Engagement.csproj" />
0014      <Project Path="src/Modules/Nexora.Modules.Finance/Nexora.Modules.Finance.csproj" />
0015      <Project Path="src/Modules/Nexora.Modules.Events/Nexora.Modules.Events.csproj" />
0016      <Project Path="src/Modules/Nexora.Modules.Membership/Nexora.Modules.Membership.csproj" />
0017      <Project Path="src/Modules/Nexora.Modules.Crm/Nexora.Modules.Crm.csproj" />
0018      <Project Path="src/Modules/Nexora.Modules.Identity/Nexora.Modules.Identity.csproj" />
0019    </Folder>
0020    <Folder Name="/tests/">
0021      <Project Path="tests/Nexora.Api.Tests/Nexora.Api.Tests.csproj" />
0022      <Project Path="tests/Nexora.Architecture.Tests/Nexora.Architecture.Tests.csproj" />
0023    </Folder>
0024  </Solution>
```
