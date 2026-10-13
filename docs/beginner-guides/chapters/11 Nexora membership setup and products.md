# Nexora membership setup and products

Document 11 of the Nexora learning guides  
Source snapshot 13 October 2026 at 17:12 UTC


Understand lookup values, membership products, prices and portal product settings.

### Recognise the screen and its scope

Open `/membership/setup`, `/membership/setup/[kind]`. CRM routes use port 3017; member routes use 3018; public website routes can use 3019. Brackets name a route parameter, not text to paste into a browser. Choose a real record or site id from the application.

This guide covers Member Type; Member Category; Member Subscription Band; Member Rate Type; Member Rate; Member Status; Member Application Status; Membership products and portal eligibility settings.

![File journey for Nexora membership setup and products](../assets/platform/guide-11-map.png)

The diagram shows the file roles. The full paths below are what you paste into Ctrl+P.

### Find the files in VS Code

| Role | File and source ID | What it does |
| --- | --- | --- |
| Screen | S0113 membership-setup.tsx | Main UI or route for this topic |
| Server | S0511 MembershipModule.cs | Endpoints or server rules |
| Data | S0417 MembershipEntities.cs | Data shape or persistence structure |

**Screen S0113**  
[apps/web/src/features/membership/membership-setup.tsx](<D:/Projects new/Nexora/apps/web/src/features/membership/membership-setup.tsx>)

**Server S0511**  
[backend/src/Modules/Nexora.Modules.Membership/MembershipModule.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Membership/MembershipModule.cs>)

**Data S0417**  
[backend/src/Modules/Nexora.Modules.Membership/Domain/MembershipEntities.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Membership/Domain/MembershipEntities.cs>)

The complete HTML manual opens these source IDs inside its Source library. The Word manual shows short labelled extracts so you can learn the flow before reading large files. Each individual Markdown guide includes its complete primary files after the walkthrough.

### Follow the request and the records

1. The setup route checks that the kind is in setupKinds.
2. MembershipSetup loads /setup/{kind}.
3. Authorised staff add or patch lookup names and descriptions.
4. Product settings specify term, rate, currency and portal application options.
5. The server stores these values and validates application requests against the product.

### Read the code with an explanation

The seven setup-kind keys are identifiers used by routes and stored records; their labels are the words shown to people. A product connects business settings such as term and rate to an application. Changing a display label differs from changing a key. Existing prices and classifications need a considered migration or business update, not a global text replacement.

Source reference S0113 apps/web/src/features/membership/membership-setup.tsx

Selected opening lines with their original numbers. The complete file is in the HTML source library.

```tsx
0001  "use client";
0002  import { EntityHeading } from "@/components/ui/entity-heading";
0003  import { useEffect, useState } from "react";
0004  import { moduleApi } from "../shared/module-api";
0005  import { ColumnChooser } from "../shared/column-chooser";
0006  import { useEntityListing } from "../shared/entity-listing-controls";
```


The import lines lead to shared components and helpers. Start at the exported component or route function; follow the request helper to the server rather than assuming the whole feature lives in this file.

Source reference S0511 backend/src/Modules/Nexora.Modules.Membership/MembershipModule.cs

Extract from source line 54. A fragment from a long line is shown without the rest of that line.

```cs
0054          api.MapGet("/setup/{kind}", async (string kind, MembershipDbContext db, CancellationToken ct) =>
```


On the server, look for MapGet to load records, MapPost to create or perform an action, MapPut or MapPatch to change data, and RequireAuthorization for permissions. Some shared methods register endpoints indirectly. Find SaveChangesAsync to see the database write. A request method alone does not tell you every side effect.

Source reference S0417 backend/src/Modules/Nexora.Modules.Membership/Domain/MembershipEntities.cs

Selected opening lines with their original numbers. The complete file is in the HTML source library.

```cs
0001  using Nexora.BuildingBlocks.Domain;
0002  namespace Nexora.Modules.Membership.Domain;
0003  public sealed class MembershipProduct : TenantEntity
0004  {
0005      public string Name { get; set; } = "";
0006      public string Category { get; set; } = "";
```


Follow model fields to their request names and database mapping. An id points to a particular record. A tenant id limits the organisation. A version or concurrency stamp protects newer edits. Some model files describe contracts rather than tables; check the owning DbContext before assuming storage.

### Make or trace your first change

1. Press Ctrl+P and open [apps/web/src/features/membership/membership-setup.tsx](<D:/Projects new/Nexora/apps/web/src/features/membership/membership-setup.tsx>).
2. Press Ctrl+F and search for `Lookup values`. In this snapshot it appears at line 20.
3. Replace the Panel title only. Open one setup list and confirm its stored names still load. Do not change the kind passed to /setup.
4. Replace only `Lookup values` with `Setup values` in that intended location. Keep quotes, tags, JSON keys and routes intact.
5. Save with Ctrl+S. Inspect Problems and the local screen. Review the diff and restore the original text when you finish practising.

Extract from source line 20. A fragment from a long line is shown without the rest of that line.

```tsx
0020   <Panel title="Lookup values">{listing.toolbar}{loading ? <p role="status
```


These exercises have not been applied to the application. Their search phrases are verified against the captured source. Line numbers can move; the phrase and file path are your main navigation aids.

### Check a change and diagnose problems

Confirm the original screen still loads, the changed words appear in the intended place, and another screen using the same component still behaves correctly. If an action fails, inspect the browser Network response, the relevant helper and server validation message. Use the common error table at the beginning of the manual. Reload on a version conflict instead of removing the version check.

A syntax error after a text edit usually means a quote, brace or closing tag changed. Compare your diff with the original. An unchanged browser result can mean the wrong port, a saved published configuration overriding a default, or a running API that has not reloaded.

### Understand the current limits

Lookup edits need configuration permission. A product rate is a stored quote or arrangement; no card processor is connected by changing it. Product portal options determine which applications are offered.

### Continue learning

Related guides [09 Nexora member records and subscriptions](#guide_09), [10 Nexora membership applications and decisions](#guide_10), [15 Nexora sales and finance](#guide_15), [21 Nexora automation controllers](#guide_21).

## Complete primary source files

These are the complete captured files, with numbered source lines. Other supporting files are included in the complete HTML manual. Configured local passwords, if any, are explicitly omitted. Do not paste the display line numbers into code.

### S0113 membership-setup.tsx

[apps/web/src/features/membership/membership-setup.tsx](<D:/Projects new/Nexora/apps/web/src/features/membership/membership-setup.tsx>)

SHA256 `875c7d1d266bbc8f628f65f6ba6b65432413a5bea9ae2de77e9e1d15cec63a76`; 23 source lines; complete captured text.

```tsx
0001  "use client";
0002  import { EntityHeading } from "@/components/ui/entity-heading";
0003  import { useEffect, useState } from "react";
0004  import { moduleApi } from "../shared/module-api";
0005  import { ColumnChooser } from "../shared/column-chooser";
0006  import { useEntityListing } from "../shared/entity-listing-controls";
0007  import { SortableHeader, parseTableSort, sortTableRows, type TableSort } from "../shared/table-sort";
0008  import { Field, Panel, inputClass, primaryClass, buttonClass } from "../shared/module-ui";
0009  type Value = {id: string; name: string; description: string; version: string};
0010  export function MembershipSetup({kind, label, canEdit}: {kind: string; label: string; canEdit: boolean}) {
0011   const [sort,setSort]=useState<TableSort>({key:"name",direction:"ascending"});
0012   const [rows, setRows] = useState<Value[]>([]);
0013   const [editing, setEditing] = useState<Value | null>(null);
0014   const [error, setError] = useState(""); const [busy, setBusy] = useState(false);
0015   const [loading, setLoading] = useState(true); const [refresh, setRefresh] = useState(0);
0016   const listing = useEntityListing({records: rows, columns: ["Name", "Description"]});
0017   useEffect(() => {let active = true; moduleApi<Value[]>("membership", `/setup/${kind}`).then(result => {if(active) {setRows(result); setError("");}}).catch(e => {if(active) setError(e.message);}).finally(() => {if(active) setLoading(false);}); return () => {active = false;};}, [kind, refresh]);
0018   return <main className="mx-auto max-w-7xl space-y-5 p-4 sm:p-7"><header><p className="text-sm text-blue-600">Membership / Setup</p><EntityHeading className="mt-2">{label}</EntityHeading></header>
0019   {error && <div role="alert" className="rounded-lg bg-red-50 p-4 text-red-800">{error} <button className={buttonClass} onClick={() => setRefresh(v => v+1)}>Reload</button></div>}
0020   <Panel title="Lookup values">{listing.toolbar}{loading ? <p role="status">Loading lookup values…</p> : <div className="overflow-x-auto"><table className="w-full text-left text-sm"><thead><tr><SortableHeader label="Name" column="name" sort={sort} onSort={value=>setSort(parseTableSort(value))}/><SortableHeader label="Description" column="description" hidden={!listing.show("Description")} sort={sort} onSort={value=>setSort(parseTableSort(value))}/>{canEdit && <th className="p-3">Actions</th>}</tr></thead><tbody>{sortTableRows(rows.filter(listing.matches),sort,(row,key)=>key==="name"?row.name:row.description).map(row => <tr key={row.id} className="border-t border-slate-200"><td className="p-3">{row.name}</td><td className="p-3 whitespace-pre-wrap" hidden={!listing.show("Description")}>{row.description}</td>{canEdit && <td className="p-3"><button className={buttonClass} onClick={() => setEditing(row)} aria-label={`Edit ${row.name}`}>Edit</button></td>}</tr>)}</tbody></table>{!rows.filter(listing.matches).length && <p className="p-3 text-sm">No matching lookup values.</p>}</div>}<footer className="mt-4 flex items-center gap-3 border-t border-slate-200 pt-3"><ColumnChooser>{listing.columnControls}</ColumnChooser><p className="text-sm">{rows.filter(listing.matches).length} records</p></footer></Panel>
0021   {canEdit && <Panel title={editing ? "Edit lookup value" : "Add lookup value"}><form key={editing?.version ?? "new"} className="space-y-3" onSubmit={async event => {event.preventDefault(); const form = event.currentTarget; const data = new FormData(form); setBusy(true);setError("");try {await moduleApi("membership", `/setup/${kind}${editing ? `/${editing.id}` : ""}`, editing ? "PATCH" : "POST", {name: data.get("name"), description: data.get("description"), version: editing?.version});setEditing(null);form.reset();setRefresh(v => v+1);}catch(e){setError(e instanceof Error ? e.message : "Unable to save.");}finally{setBusy(false);}}}><Field label="Name"><input name="name" required maxLength={160} defaultValue={editing?.name} className={inputClass} /></Field><Field label="Description"><textarea name="description" maxLength={2000} defaultValue={editing?.description} className={inputClass} rows={3} /></Field><div className="flex gap-2"><button className={primaryClass} disabled={busy}>Save lookup value</button>{editing && <button type="button" className={buttonClass} disabled={busy} onClick={() => setEditing(null)}>Cancel edit</button>}</div></form></Panel>}
0022   </main>;
0023  }
```

### S0511 MembershipModule.cs

[backend/src/Modules/Nexora.Modules.Membership/MembershipModule.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Membership/MembershipModule.cs>)

SHA256 `4d87db514a0a765a297d1fbc46cad37a3143d0c9c0f4dd1be52413f9302e1a67`; 238 source lines; complete captured text.

```cs
0001  using Nexora.BuildingBlocks.Persistence;
0002  using Nexora.BuildingBlocks.Reporting;
0003  using Microsoft.AspNetCore.Builder;
0004  using Microsoft.AspNetCore.Http;
0005  using Microsoft.AspNetCore.Routing;
0006  using Microsoft.EntityFrameworkCore;
0007  using Microsoft.Extensions.Configuration;
0008  using Microsoft.Extensions.DependencyInjection;
0009  using Nexora.BuildingBlocks.Endpoints;
0010  using Nexora.BuildingBlocks.Security;
0011  using Nexora.Modules.Membership.Domain;
0012  using Nexora.Modules.Membership.Infrastructure;
0013  using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
0014  namespace Nexora.Modules.Membership;
0015  public static partial class MembershipModule
0016  {
0017      public sealed record SetupValueRequest(string Name, string Description, Guid? Version);
0018      private static void ValidateSetupKind(string kind) => Choice(kind, "member-type", "member-category", "member-subscription-band", "member-rate-type", "member-rate", "member-status", "member-application-status");
0019      private static void ValidateSetupValue(SetupValueRequest r)
0020      {
0021          Text(r.Name);
0022          Check(r.Description != null && r.Description.Length <= 2000, "Description must not exceed 2000 characters.");
0023      }
0024      private static (int Number, int Size, int Offset) Pagination(string? page, string? pageSize)
0025      {
0026          var number = 1;
0027          var size = 25;
0028          Check(page == null || int.TryParse(page, out number), "Page must be a whole number.");
0029          Check(pageSize == null || int.TryParse(pageSize, out size), "Page size must be a whole number.");
0030          Check(number >= 1 && size is >= 1 and <= 100, "Use a page of at least 1 and a page size of 1â€“100.");
0031          var offset = ((long)number - 1) * size;
0032          Check(offset <= int.MaxValue, "The requested page is too large.");
0033          return (number, size, (int)offset);
0034      }
0035      public static IServiceCollection AddNexoraMembership(this IServiceCollection services, IConfiguration config)
0036      { services.AddScoped<Nexora.BuildingBlocks.Security.ICrmContextProvider, Nexora.Modules.Membership.Infrastructure.CrmContextProvider>();
0037          services.AddScoped<Nexora.BuildingBlocks.Security.ICrmContextProvider, Nexora.Modules.Membership.Infrastructure.CrmEvidenceContext>();
0038          AddWorkflowCommands(services);
0039          services.AddDbContext<MembershipDbContext>((sp,o)=>o.AddInterceptors(sp.GetRequiredService<WorkflowCaptureInterceptor>()).UseSqlServer(config.GetConnectionString("Nexora"), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory","membership"))); services.AddScoped<IReportDataset>(sp=>new ReportDataset("members","Memberships","membership.read",sp.GetRequiredService<MembershipDbContext>().Set<MemberSubscription>().AsNoTracking().Select(x=>new ReportRow{Id=x.Id,Label=x.ContactName,Status=x.Status,CreatedAtUtc=x.CreatedAtUtc})));
0040          services.AddScoped<IMemberMembershipDirectory, MemberMembershipDirectory>();
0041          services.AddScoped<ICrmMembershipDirectory, CrmMembershipDirectory>();
0042          services.AddScoped<IMemberApplicationService, MemberApplicationService>();
0043          services.AddScoped<IPortalNotificationService, PortalNotificationService>();
0044          services.AddScoped<IApplicationEvidenceService, ApplicationEvidenceService>();
0045          services.AddScoped<Nexora.BuildingBlocks.Documents.IDocumentScanner, Nexora.BuildingBlocks.Documents.ClamDocumentScanner>();
0046  
0047          services.AddWorkflowEntity<MembershipDbContext,MembershipApplication>("applications","Membership applications","membership.read","ContactName",null,null,null,new WorkflowRelation("contact","contacts","contactId","id",false),new WorkflowRelation("account","accounts","accountId","id",false),new WorkflowRelation("product","membership-products","productId","id",false));
0048          services.AddWorkflowEntity<MembershipDbContext,MemberSubscription>("members","Members","membership.read","ContactName",null,null,null,new WorkflowRelation("contact","contacts","contactId","id",false),new WorkflowRelation("account","accounts","accountId","id",false),new WorkflowRelation("product","membership-products","productId","id",false));
0049          services.AddWorkflowEntity<MembershipDbContext,MembershipProduct>("membership-products","Membership offerings","membership.read","Name",null,null,null);
0050          return services; }
0051      public static IEndpointRouteBuilder MapNexoraMembership(this IEndpointRouteBuilder routes)
0052      {
0053          var api = routes.Module("membership").WithAttributes<MembershipDbContext>("membership.manage", typeof(MembershipProduct), typeof(MembershipApplication), typeof(MemberSubscription));
0054          api.MapGet("/setup/{kind}", async (string kind, MembershipDbContext db, CancellationToken ct) =>
0055          {
0056              ValidateSetupKind(kind);
0057              return await db.Set<MembershipSetupValue>().AsNoTracking().Where(x => x.Kind == kind).OrderBy(x => x.Name).ToListAsync(ct);
0058          });
0059          api.MapPost("/setup/{kind}", async (string kind, SetupValueRequest r, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
0060          {
0061              ValidateSetupKind(kind); ValidateSetupValue(r);
0062              var row = new MembershipSetupValue { TenantId = who.TenantId!.Value, Kind = kind, Name = Text(r.Name), Description = r.Description.Trim() };
0063              db.Add(row); History(db, who, http, row.Id, "setup.created", "Created " + kind + " lookup");
0064              await db.SaveChangesAsync(ct); return Results.Ok(row);
0065          }).RequireAuthorization("membership.manage");
0066          api.MapPatch("/setup/{kind}/{id:guid}", async (string kind, Guid id, SetupValueRequest r, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
0067          {
0068              ValidateSetupKind(kind); ValidateSetupValue(r);
0069              var row = await db.Set<MembershipSetupValue>().SingleOrDefaultAsync(x => x.Id == id && x.Kind == kind, ct) ?? throw new KeyNotFoundException();
0070              if (row.Version != r.Version) return Conflict();
0071              row.Name = Text(r.Name); row.Description = r.Description.Trim();
0072              History(db, who, http, id, "setup.updated", "Updated " + kind + " lookup");
0073              await db.SaveChangesAsync(ct); return Results.Ok(row);
0074          }).RequireAuthorization("membership.manage");
0075          api.MapMembershipOperations();
0076          api.MapRejoinCandidates();
0077          MapMembershipAdministration(api);
0078          MapMembershipApplicationDetails(api);
0079          MapMembershipArchive(api);
0080          MapMembershipOptions(api);
0081          MapMembershipTimeline(api);
0082          MapMembershipFinancialArrangement(api);
0083          MapMembershipJoinDate(api);
0084          MapMembershipClassifications(api);
0085          api.MapApplicationEvidence();
0086          api.MapPut("/products/{id:guid}/portal", async (Guid id, PortalProductSettings r, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
0087          {
0088              var product = await db.Set<MembershipProduct>().SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new KeyNotFoundException();
0089              if (product.Version != r.Version) return Conflict();
0090              Check(!(r.Enabled || r.OrganisationEnabled) || !string.IsNullOrWhiteSpace(r.Terms), "Provide member-facing terms before enabling applications.");
0091              Check(r.Terms != null && r.Terms.Length <= 4000, "Terms must not exceed 4000 characters.");
0092              product.PortalApplicationsEnabled = r.Enabled; product.PortalOrganisationApplicationsEnabled = r.OrganisationEnabled; product.PortalTerms = r.Terms!.Trim();
0093              History(db, who, http, id, "product.portal-settings", "Updated portal availability and terms");
0094              await db.SaveChangesAsync(ct); return Results.Ok(product);
0095          }).RequireAuthorization("membership.manage");
0096          api.MapGet("/contacts", (string? search, ICrmDirectory crm, CancellationToken ct) => crm.SearchContactsAsync(search,ct));
0097          api.MapGet("/accounts", (string? search, ICrmDirectory crm, CancellationToken ct) => crm.SearchAccountsAsync(search,ct));
0098          api.MapGet("/account-contact", async (Guid accountId, Guid contactId, ICrmDirectory crm, CancellationToken ct) => Results.Ok(new { linked = await crm.HasCurrentAccountContactAsync(accountId, contactId, ct) }));
0099          api.MapGet("/products", async (MembershipDbContext db, CancellationToken ct) => await db.Set<MembershipProduct>().OrderBy(x => x.Name).ToListAsync(ct));
0100          api.MapGet("/portal-notifications", async (MembershipDbContext db, CancellationToken ct) =>
0101              await db.Set<PortalNotification>().AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(ct));
0102          api.MapPost("/portal-notifications/{id:guid}/deliver", async (Guid id, MembershipDbContext db, CancellationToken ct) =>
0103          {
0104              var row = await db.Set<PortalNotification>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
0105              Check(row.DeliveryStatus == "queued", "Only queued notifications can be delivered.");
0106              row.DeliveryStatus = "delivered"; row.DeliveryAttempts++;
0107              await db.SaveChangesAsync(ct); return Results.Ok(new { row.Id, row.DeliveryStatus, row.DeliveryAttempts });
0108          }).RequireAuthorization("membership.manage");
0109          api.MapPost("/products", async (ProductRequest r, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
0110          {
0111              ValidateProduct(r);
0112              var product = new MembershipProduct { TenantId = who.TenantId!.Value, Name = Text(r.Name), Category = Text(r.Category,80), TermMonths = r.TermMonths, Rate = r.Rate, Currency = r.Currency, IsActive = r.IsActive };
0113              db.Add(product); History(db,who,http,product.Id,"product.created","Created product"); await db.SaveChangesAsync(ct); return Results.Ok(product);
0114          }).RequireAuthorization("membership.manage");
0115          api.MapPatch("/products/{id:guid}", async (Guid id, ProductRequest r, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
0116          {
0117              var product = await db.Set<MembershipProduct>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
0118              if (r.Version != product.Version) return Conflict(); ValidateProduct(r);
0119              product.Name = Text(r.Name); product.Category = Text(r.Category,80); product.TermMonths = r.TermMonths; product.Rate = r.Rate; product.Currency = r.Currency; product.IsActive = r.IsActive;
0120              History(db,who,http,id,"product.updated","Updated catalogue; existing quotes unchanged"); await db.SaveChangesAsync(ct); return Results.Ok(product);
0121          }).RequireAuthorization("membership.manage");
0122          api.MapGet("/applications", async (MembershipDbContext db, string? search, string? status, string? page, string? pageSize, string? sort, CancellationToken ct) =>
0123          {
0124              var (number, size, offset) = Pagination(page, pageSize);
0125              var q = db.Set<MembershipApplication>().AsNoTracking();
0126              if (!string.IsNullOrWhiteSpace(search)) { var term = Text(search); q = q.Where(x => (x.ContactName.Contains(term) || (x.AccountName != null && x.AccountName.Contains(term)))); }
0127              if (!string.IsNullOrWhiteSpace(status)) { Choice(status,"submitted","approved","rejected"); q = q.Where(x => x.Status == status); }
0128              var totalCount = await q.CountAsync(ct);
0129              var items = await MembershipListingSorting.Applications(q, db, sort).ThenBy(x => x.Id).Skip(offset).Take(size).ToListAsync(ct);
0130              return Results.Ok(new { items, totalCount, page = number, pageSize = size });
0131          });
0132          api.MapGet("/applications/{id:guid}", async (Guid id, MembershipDbContext db, CancellationToken ct) => await db.Set<MembershipApplication>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) is { } application ? Results.Ok(application) : Results.NotFound());
0133          api.MapPost("/applications", async (ApplicationRequest r, MembershipDbContext db, ICrmDirectory crm, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
0134          {
0135              var application = await MembershipApplicationCommand.CreateAsync(r, db, crm, who, http.TraceIdentifier, ct);
0136              await db.SaveChangesAsync(ct); return Results.Ok(application);
0137          }).RequireAuthorization("membership.manage");
0138          api.MapPost("/applications/{id:guid}/decision", async (Guid id, DecisionRequest r, MembershipDbContext db, ICrmDirectory crm, IPortalRecipientDirectory recipients, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
0139          {
0140              var application = await db.Set<MembershipApplication>().SingleOrDefaultAsync(x => x.Id == id,ct) ?? throw new KeyNotFoundException();
0141              if (application.Version != r.Version) return Conflict(); Choice(r.Decision,"approve","reject"); Check(application.Status == "submitted","This application already has a decision.");
0142              await ApplyDecision(application,r,db,crm,recipients,who,http,ct);
0143              await db.SaveChangesAsync(ct); return Results.Ok(application);
0144          }).RequireAuthorization("membership.decide");
0145          api.MapGet("/members", async (MembershipDbContext db, string? search, string? status, string? archive, string? page, string? pageSize, string? sort, CancellationToken ct) =>
0146          {
0147              var (number, size, offset) = Pagination(page, pageSize);
0148              var q = db.Set<MemberSubscription>().AsNoTracking();
0149              var archiveFilter = string.IsNullOrWhiteSpace(archive) ? "current" : archive; Choice(archiveFilter,"current","archived","all");
0150              if (archiveFilter != "all") q = q.Where(x => x.IsArchived == (archiveFilter == "archived"));
0151              if (!string.IsNullOrWhiteSpace(search)) { var term = Text(search); q = q.Where(x => (x.ContactName.Contains(term) || (x.AccountName != null && x.AccountName.Contains(term)))); }
0152              if (!string.IsNullOrWhiteSpace(status)) { Choice(status,"active","cancelled","lapsed"); q = q.Where(x => x.Status == status); }
0153              var totalCount = await q.CountAsync(ct);
0154              var items = await MembershipListingSorting.Members(q, db, sort).ThenBy(x => x.Id).Skip(offset).Take(size).ToListAsync(ct);
0155              return Results.Ok(new { items, totalCount, page = number, pageSize = size });
0156          });
0157          api.MapGet("/members/{id:guid}", async (Guid id, MembershipDbContext db, CancellationToken ct) =>
0158          {
0159              var member = await db.Set<MemberSubscription>().SingleOrDefaultAsync(x => x.Id == id,ct) ?? throw new KeyNotFoundException();
0160              return Results.Ok(new { member, history = await db.Set<MembershipHistory>().Where(x => x.SubjectId == id).OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(ct) });
0161          });
0162          api.MapPost("/members/{id:guid}/{action}", async (Guid id, string action, ChangeRequest r, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
0163          {
0164              var member = await db.Set<MemberSubscription>().SingleOrDefaultAsync(x => x.Id == id,ct) ?? throw new KeyNotFoundException();
0165              if (r.Version != member.Version) return Conflict(); Choice(action,"renew","cancel","resign","lapse","reinstate"); var reason = Text(r.Reason,500);
0166              await ApplyMemberChange(member,action,reason,db,who,http,ct,r.EffectiveOn); await db.SaveChangesAsync(ct); return Results.Ok(member);
0167          }).RequireAuthorization("membership.manage");
0168          api.MapGet("/history/{id:guid}", async (Guid id, MembershipDbContext db, CancellationToken ct) => await db.Set<MembershipHistory>().Where(x => x.SubjectId == id).OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(ct));
0169          return routes;
0170      }
0171      private static async Task ApplyDecision(MembershipApplication application,DecisionRequest r,MembershipDbContext db,ICrmDirectory crm,IPortalRecipientDirectory recipients,IRequestIdentity who,HttpContext http,CancellationToken ct)
0172      {
0173          Choice(r.Decision,"approve","reject");Check(application.Status=="submitted","This application already has a decision.");
0174              application.DecisionReason = Text(r.Reason,500);
0175              if (r.Decision == "approve")
0176              {
0177                  Check(await crm.FindActiveAsync(application.ContactId,ct) != null,"The Contact is no longer active.");
0178                  if (application.AccountId.HasValue)
0179                  {
0180                      Check(await crm.FindActiveAsync(application.AccountId.Value, ct) != null, "The organisation is no longer active.");
0181                      Check(await crm.HasCurrentAccountContactAsync(application.AccountId.Value, application.ContactId, ct), "The Contact is no longer linked to this organisation.");
0182                  }
0183                  var previous = await MembershipRejoin.ValidateAsync(application.PreviousMembershipId, application.ContactId, application.AccountId, db, ct);
0184                  var requestedEnd = application.RequestedEndUtc ?? application.RequestedStartUtc.AddMonths(application.TermMonths);
0185                  Check(!await db.Set<MemberSubscription>().AnyAsync(x =>
0186                      (application.AccountId.HasValue ? x.AccountId == application.AccountId : x.AccountId == null && x.ContactId == application.ContactId)
0187                      && x.Status == "active",ct),
0188                      "This applicant already has an active membership. End it before activating another membership.");
0189                  var member = new MemberSubscription { FreeMembership = application.FreeMembership, ExemptJoiningFee = application.ExemptJoiningFee, PreviousMembershipId = previous?.Id, RejoinedOn = previous != null ? DateOnly.FromDateTime(DateTime.UtcNow) : null, OriginalJoinDate = previous?.OriginalJoinDate, StatusEffectiveOn = DateOnly.FromDateTime(DateTime.UtcNow), TenantId = who.TenantId!.Value, AccountId = application.AccountId, AccountName = application.AccountName, ContactId = application.ContactId, ContactName = application.ContactName, ProductId = application.ProductId, StartsAtUtc = application.RequestedStartUtc, EndsAtUtc = requestedEnd, Rate = application.QuotedRate, Currency = application.Currency, ApplicationId = application.Id };
0190                  db.Add(member);
0191                  await ApplicationDetailsFields.ApplyApprovalAsync(application, member, db, crm, who, ct);
0192                  application.MembershipId = member.Id; History(db,who,http,member.Id,"membership.activated",r.Reason,effectiveOn: member.StatusEffectiveOn);
0193                  if (previous != null) History(db,who,http,member.Id,"membership.rejoin",application.DecisionReason,effectiveOn: member.RejoinedOn);
0194              }
0195              application.Status = r.Decision == "approve" ? "approved" : "rejected"; application.StatusEffectiveOn = DateOnly.FromDateTime(DateTime.UtcNow); History(db,who,http,application.Id,"application." + application.Status,r.Reason,effectiveOn: application.StatusEffectiveOn);
0196              var recipient = await recipients.FindPortalUserIdAsync(who.TenantId!.Value, application.ContactId, ct);
0197              if (recipient is Guid userId) db.Add(new PortalNotification { TenantId = who.TenantId.Value, RecipientUserId = userId, ApplicationId = application.Id, Kind = "application." + application.Status, Subject = application.Status == "approved" ? "Application approved" : "Application rejected", Body = application.Status == "approved" ? "Your application was approved." : "Your application was rejected.", DeliveryStatus = "queued" });
0198      }
0199      private static async Task ApplyMemberChange(MemberSubscription member,string action,string reason,MembershipDbContext db,IRequestIdentity who,HttpContext http,CancellationToken ct,DateOnly? effectiveOn = null)
0200      {
0201          Choice(action,"renew","cancel","resign","lapse","reinstate");
0202              Check(!member.IsArchived,"Restore the membership before changing its lifecycle.");
0203              var today = DateOnly.FromDateTime(DateTime.UtcNow);
0204              var effective = effectiveOn ?? today;
0205              Check(effective != DateOnly.MinValue && effective <= today,"Choose a valid effective date on or before today.");
0206              Check(action is not ("renew" or "reinstate") || effective == today,"Renewal and reinstatement use today's effective date and current term policy.");
0207              var previousEffective = await db.Set<MembershipHistory>().Where(x => x.SubjectId == member.Id && x.EffectiveOn != null).MaxAsync(x => x.EffectiveOn, ct);
0208              Check(member.OriginalJoinDate == null || effective >= member.OriginalJoinDate,"Effective date cannot precede the recorded original join date.");
0209              Check(member.StatusEffectiveOn == null || effective >= member.StatusEffectiveOn,"Effective date cannot precede the current status date.");
0210              Check(previousEffective == null || effective >= previousEffective,"Effective date cannot precede the latest recorded lifecycle date.");
0211              var previousStatus = member.Status;
0212              var previousEnd = member.EndsAtUtc;
0213              if (action is "renew" or "reinstate")
0214              {
0215                  Check(action == "renew" ? member.Status is "active" or "lapsed" : member.Status == "cancelled", "This transition is not allowed from the current status.");
0216                  Check(!await db.Set<MemberSubscription>().AnyAsync(x => x.Id != member.Id && x.Status == "active" &&
0217                      (member.AccountId.HasValue ? x.AccountId == member.AccountId : x.AccountId == null && x.ContactId == member.ContactId), ct),
0218                      "This applicant already has an active membership. End it before activating another membership.");
0219                  Check(!await db.Set<MemberSubscription>().AnyAsync(x => x.PreviousMembershipId == member.Id, ct), "This membership has a rejoin successor. Renew or reinstate the latest membership instead.");
0220                  var quote = await MembershipOperations.RenewalQuoteAsync(db, member, action == "reinstate", ct);
0221                  member.EndsAtUtc = quote.EndsAtUtc; member.Rate = quote.Total; member.Currency = quote.Currency; member.Status = "active";
0222              }
0223              else { Check(member.Status == "active","Only active memberships can be cancelled or lapsed."); if (action == "lapse") Check(member.ExcludedFromLapse != true,"This membership is excluded from lapse. Update its options first."); if (action == "lapse") Check(member.EndsAtUtc <= DateTime.UtcNow,"A membership cannot lapse before its term ends."); if (action == "lapse") Check(effective >= DateOnly.FromDateTime(member.EndsAtUtc),"Lapse effective date cannot precede the term end."); member.Status = action is "cancel" or "resign" ? "cancelled" : "lapsed"; }
0224              if (member.Status != previousStatus) member.StatusEffectiveOn = effective;
0225              if (action == "resign") { member.ResignedOn = effective; member.ResignationReason = reason; }
0226              if (action == "reinstate") member.ReinstatedOn = effective;
0227              History(db,who,http,member.Id,"membership." + action,reason,previousEnd,member.EndsAtUtc,effective);
0228      }
0229      private static string? TextOptional(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : Text(value, max);
0230      private static void ValidateProduct(ProductRequest r) { Text(r.Name); Text(r.Category,80); Check(r.TermMonths is >= 1 and <= 60,"Terms must be between 1 and 60 months."); Check(r.Rate >= 0 && r.Rate < 100000000 && decimal.Round(r.Rate,2) == r.Rate,"Enter a nonnegative rate with two decimal places."); Choice(r.Currency,"GBP","EUR","USD"); }
0231      private static void History(MembershipDbContext db, IRequestIdentity who, HttpContext http, Guid id, string action, string reason, DateTime? previous = null, DateTime? next = null, DateOnly? effectiveOn = null) => db.Add(new MembershipHistory { TenantId = who.TenantId!.Value, SubjectId = id, ActorUserId = who.UserId!.Value, Action = action, Reason = reason, CorrelationId = http.TraceIdentifier, PreviousEndUtc = previous, NewEndUtc = next, EffectiveOn = effectiveOn });
0232  }
0233  public sealed record ProductRequest(string Name,string Category,int TermMonths,decimal Rate,string Currency,bool IsActive,Guid? Version) : IAttributeRequest { public Dictionary<Guid, AttributeInput>? Attributes { get; init; } }
0234  public sealed record ApplicationRequest(Guid ContactId,Guid? AccountId,Guid ProductId,DateTime StartsAtUtc,string ApplicationType="corporate",string? ApplicantRole=null,bool TermsAccepted=false,string? ApplicantNotes=null,Guid? PreviousMembershipId=null,bool? ExemptJoiningFee=null,bool? FreeMembership=null, ApplicationRequestedDetails? RequestedDetails=null, string? LifecyclePurpose=null) : IAttributeRequest { public Dictionary<Guid, AttributeInput>? Attributes { get; init; } }
0235  public sealed record DecisionRequest(Guid Version,string Decision,string Reason);
0236  public sealed record PortalProductSettings(Guid Version, bool Enabled, string Terms, bool OrganisationEnabled = false);
0237  public sealed record ChangeRequest(Guid Version,string Reason,DateOnly? EffectiveOn = null);
0238  
```

### S0417 MembershipEntities.cs

[backend/src/Modules/Nexora.Modules.Membership/Domain/MembershipEntities.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Membership/Domain/MembershipEntities.cs>)

SHA256 `83d9b09d6a02aa3fa9a771ace99e4f12f93a3e51de6cd12aa0cb1d74352421cd`; 134 source lines; complete captured text.

```cs
0001  using Nexora.BuildingBlocks.Domain;
0002  namespace Nexora.Modules.Membership.Domain;
0003  public sealed class MembershipProduct : TenantEntity
0004  {
0005      public string Name { get; set; } = "";
0006      public string Category { get; set; } = "";
0007      public int TermMonths { get; set; } = 12;
0008      public decimal Rate { get; set; }
0009      public string Currency { get; set; } = "GBP";
0010      public bool IsActive { get; set; } = true;
0011      public bool PortalApplicationsEnabled { get; set; }
0012      public bool PortalOrganisationApplicationsEnabled { get; set; }
0013      public string PortalTerms { get; set; } = "";
0014  }
0015  public sealed class PortalApplicationDraft : TenantEntity
0016  {
0017      public Guid? AccountId { get; set; }
0018      public Guid ContactId { get; set; }
0019      public Guid ProductId { get; set; }
0020      public DateTime StartsAtUtc { get; set; }
0021      public string Notes { get; set; } = "";
0022      public Guid ProductVersion { get; set; }
0023      public Guid? PolicyVersion { get; set; }
0024      public decimal Total { get; set; }
0025      public string Currency { get; set; } = "GBP";
0026      public string Terms { get; set; } = "";
0027      public Guid? ApplicationId { get; set; }
0028  }
0029  public sealed class MembershipApplication : TenantEntity
0030  {
0031      public string? LifecyclePurpose { get; set; }
0032      public DateOnly? StatusEffectiveOn { get; set; }
0033      public Guid? RequestedMemberTypeId { get; set; }
0034      public string? RequestedMemberTypeName { get; set; }
0035      public Guid? RequestedCategoryId { get; set; }
0036      public string? RequestedCategoryName { get; set; }
0037      public Guid? RequestedSubscriptionBandId { get; set; }
0038      public string? RequestedSubscriptionBandName { get; set; }
0039      public string? PaymentMethod { get; set; }
0040      public string? PaymentFrequency { get; set; }
0041      public bool? InvoiceToAccount { get; set; }
0042      public Guid? InvoicingAccountId { get; set; }
0043      public string? InvoicingAccountName { get; set; }
0044      public string? StaffRemarks { get; set; }
0045      public string? Description { get; set; }
0046      public bool? ExemptJoiningFee { get; set; }
0047      public bool? FreeMembership { get; set; }
0048      public Guid? PreviousMembershipId { get; set; }
0049      public Guid? AccountId { get; set; }
0050      public string? AccountName { get; set; }
0051      public Guid ContactId { get; set; }
0052      public string ContactName { get; set; } = "";
0053      public Guid ProductId { get; set; }
0054      public string ApplicationType { get; set; } = "corporate";
0055      public string? ApplicantRole { get; set; }
0056      public bool TermsAccepted { get; set; }
0057      public DateTime? TermsAcceptedAtUtc { get; set; }
0058      public string? ApplicantNotes { get; set; }
0059      public string Status { get; set; } = "submitted";
0060      public DateTime RequestedStartUtc { get; set; }
0061      // Null on legacy applications: approval preserves the original term-month fallback.
0062      // New applications snapshot the quoted end date so later policy edits cannot change it.
0063      public DateTime? RequestedEndUtc { get; set; }
0064      public decimal QuotedRate { get; set; }
0065      public string Currency { get; set; } = "GBP";
0066      public int TermMonths { get; set; }
0067      public string DecisionReason { get; set; } = "";
0068      public Guid? MembershipId { get; set; }
0069  }
0070  public sealed class MemberSubscription : TenantEntity
0071  {
0072      public int? RenewalMonth { get; set; }
0073      public DateOnly? RecordedRenewalDate { get; set; }
0074      public DateOnly? NextRenewalDate { get; set; }
0075      public string? PromotionCode { get; set; }
0076      public string? PaymentMethod { get; set; }
0077      public string? PaymentFrequency { get; set; }
0078      public string? PaymentProvider { get; set; }
0079      public Guid? PaidById { get; set; }
0080      public string? PaidByName { get; set; }
0081      public string? PaidByKind { get; set; }
0082      public bool? InvoiceToAccount { get; set; }
0083      public Guid? InvoicingAccountId { get; set; }
0084      public string? InvoicingAccountName { get; set; }
0085      public bool? ExemptJoiningFee { get; set; }
0086      public bool? FreeMembership { get; set; }
0087      public bool? ExcludedFromLapse { get; set; }
0088      public bool? PrimaryMembership { get; set; }
0089      public bool IsArchived { get; set; }
0090      public DateTime? ArchivedAtUtc { get; set; }
0091      public Guid? ArchivedByUserId { get; set; }
0092      public string? ArchiveReason { get; set; }
0093      public Guid? PreviousMembershipId { get; set; }
0094      public DateOnly? RejoinedOn { get; set; }
0095      public DateOnly? OriginalJoinDate { get; set; }
0096      public DateOnly? StatusEffectiveOn { get; set; }
0097      public DateOnly? ResignedOn { get; set; }
0098      public string? ResignationReason { get; set; }
0099      public DateOnly? ReinstatedOn { get; set; }
0100      public string? LegacyMemberReference { get; set; }
0101      public bool? MembershipPackSent { get; set; }
0102      public DateOnly? MembershipPackSentDate { get; set; }
0103      public string? Remarks { get; set; }
0104      public Guid? AccountId { get; set; }
0105      public string? AccountName { get; set; }
0106      public Guid ContactId { get; set; }
0107      public string ContactName { get; set; } = "";
0108      public Guid ProductId { get; set; }
0109      public string Status { get; set; } = "active";
0110      public DateTime StartsAtUtc { get; set; }
0111      public DateTime EndsAtUtc { get; set; }
0112      public decimal Rate { get; set; }
0113      public string Currency { get; set; } = "GBP";
0114      public Guid ApplicationId { get; set; }
0115  }
0116  public sealed class MembershipHistory : TenantEntity, IAppendOnly
0117  {
0118      public DateOnly? EffectiveOn { get; set; }
0119      public Guid SubjectId { get; set; }
0120      public Guid ActorUserId { get; set; }
0121      public string Action { get; set; } = "";
0122      public string Reason { get; set; } = "";
0123      public string CorrelationId { get; set; } = "";
0124      public DateTime? PreviousEndUtc { get; set; }
0125      public DateTime? NewEndUtc { get; set; }
0126  }
0127  
0128  
0129  public sealed class MembershipSetupValue : TenantEntity
0130  {
0131   public string Kind { get; set; } = "";
0132   public string Name { get; set; } = "";
0133   public string Description { get; set; } = "";
0134  }
```
