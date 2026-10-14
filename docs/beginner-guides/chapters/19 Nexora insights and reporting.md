# Nexora insights and reporting

Document 19 of the Nexora learning guides  
Source snapshot 14 October 2026 at 06:41 UTC


Understand dataset selection, query filters, matching totals, paging and CSV export.

### Recognise the screen and its scope

Open `/insights`. CRM routes use port 3017; member routes use 3018; public website routes can use 3019. Brackets name a route parameter, not text to paste into a browser. Choose a real record or site id from the application.

This guide covers Available datasets; Search exact status and created-date filters; Server query paging; Sorting; CSV export; Tenant and role restrictions.

![File journey for Nexora insights and reporting](../assets/platform/guide-19-map.png)

The diagram shows the file roles. The full paths below are what you paste into Ctrl+P.

### Find the files in VS Code

| Role | File and source ID | What it does |
| --- | --- | --- |
| Screen | S0099 insights-workspace.tsx | Main UI or route for this topic |
| Server | S0578 ReportingEndpoints.cs | Endpoints or server rules |
| Data | S0200 ReportDataset.cs | Data shape or persistence structure |

**Screen S0099**  
[apps/web/src/features/insights/insights-workspace.tsx](<D:/Projects new/Nexora/apps/web/src/features/insights/insights-workspace.tsx>)

**Server S0578**  
[backend/src/Nexora.Api/ReportingEndpoints.cs](<D:/Projects new/Nexora/backend/src/Nexora.Api/ReportingEndpoints.cs>)

**Data S0200**  
[backend/src/BuildingBlocks/Nexora.BuildingBlocks/Reporting/ReportDataset.cs](<D:/Projects new/Nexora/backend/src/BuildingBlocks/Nexora.BuildingBlocks/Reporting/ReportDataset.cs>)

The complete HTML manual opens these source IDs inside its Source library. The Word manual shows short labelled extracts so you can learn the flow before reading large files. Each individual Markdown guide includes its complete primary files after the walkthrough.

### Follow the request and the records

1. Insights asks the API for datasets permitted to the signed-in user.
2. The user selects a dataset and enters filters.
3. A POST query request applies those filters on the server.
4. The response supplies total and current page items.
5. Export requests the permitted matching records with the same intended filter scope.

### Read the code with an explanation

A dataset describes how a module exposes reportable records. The browser has filter state and query state; pressing Run report commits the chosen filter values to a request. Previous and Next change skip while keeping the query. Total is the full matching count, while items is one page. Permission checks protect both queries and exports.

Source reference S0099 apps/web/src/features/insights/insights-workspace.tsx

Selected opening lines with their original numbers. The complete file is in the HTML source library.

```tsx
0001  "use client";
0002  import { EntityHeading } from "@/components/ui/entity-heading";
0003  import {useEffect,useState} from "react";
0004  import {moduleApi} from "@/features/shared/module-api";
0005  import { ColumnChooser } from "../shared/column-chooser";
0006  import { FilterViewToolbar } from "../shared/filter-view-toolbar";
```


The import lines lead to shared components and helpers. Start at the exported component or route function; follow the request helper to the server rather than assuming the whole feature lives in this file.

Source reference S0578 backend/src/Nexora.Api/ReportingEndpoints.cs

Extract from source line 12. A fragment from a long line is shown without the rest of that line.

```cs
0012    api.MapGet("/datasets",(IEnumerable<IReportDataset> datasets,HttpContext http)=>datasets.Where(d=>Allowed(http,d)).Select(d=>new{d.Key,d.Title,fields=new[]{"label","status","createdAtUtc"}}));
```


On the server, look for MapGet to load records, MapPost to create or perform an action, MapPut or MapPatch to change data, and RequireAuthorization for permissions. Some shared methods register endpoints indirectly. Find SaveChangesAsync to see the database write. A request method alone does not tell you every side effect.

Source reference S0200 backend/src/BuildingBlocks/Nexora.BuildingBlocks/Reporting/ReportDataset.cs

Selected opening lines with their original numbers. The complete file is in the HTML source library.

```cs
0001  using Microsoft.EntityFrameworkCore;
0002  namespace Nexora.BuildingBlocks.Reporting;
0003  public sealed record ReportQuery(string? Search=null,string? Status=null,DateTime? FromUtc=null,DateTime? ToUtc=null,int Skip=0,int Take=50,string Sort="-createdAtUtc");
0005  public sealed record ReportPage(int Total,IReadOnlyList<ReportRow> Items);
0006  public interface IReportDataset
0007  {
```


Follow model fields to their request names and database mapping. An id points to a particular record. A tenant id limits the organisation. A version or concurrency stamp protects newer edits. Some model files describe contracts rather than tables; check the owning DbContext before assuming storage.

### Make or trace your first change

1. Press Ctrl+P and open [apps/web/src/features/insights/insights-workspace.tsx](<D:/Projects new/Nexora/apps/web/src/features/insights/insights-workspace.tsx>).
2. Press Ctrl+F and search for `Explore records available to your tenant and role. Counts reflect the time this page loaded.`. In this snapshot it appears at line 19.
3. Replace only this paragraph text. Run the same report before and after the change and check the total and results do not change because of the wording.
4. Replace only `Explore records available to your tenant and role. Counts reflect the time this page loaded.` with `Explore the records your organisation and role can access. Counts show this page load.` in that intended location. Keep quotes, tags, JSON keys and routes intact.
5. Save with Ctrl+S. Inspect Problems and the local screen. Review the diff and restore the original text when you finish practising.

Extract from source line 19. A fragment from a long line is shown without the rest of that line.

```tsx
0019  ding><p className="text-slate-600">Explore records available to your tenant and role. Counts reflect the time this page loaded.</p>{error&&<p role="alert" className="text-r
```


These exercises have not been applied to the application. Their search phrases are verified against the captured source. Line numbers can move; the phrase and file path are your main navigation aids.

### Check a change and diagnose problems

Confirm the original screen still loads, the changed words appear in the intended place, and another screen using the same component still behaves correctly. If an action fails, inspect the browser Network response, the relevant helper and server validation message. Use the common error table at the beginning of the manual. Reload on a version conflict instead of removing the version check.

A syntax error after a text edit usually means a quote, brace or closing tag changed. Compare your diff with the original. An unchanged browser result can mean the wrong port, a saved published configuration overriding a default, or a running API that has not reloaded.

### Understand the current limits

An Insights count measures stored records available to that role. It does not establish external analytics integration or broader commercial KPIs. Export rights are separate from read rights.

### Continue learning

Related guides [04 Nexora home dashboard](#guide_04), [07 Nexora record listings and saved views](#guide_07), [15 Nexora sales and finance](#guide_15), [18 Nexora imports exports and migration](#guide_18).

## Complete primary source files

These are the complete captured files, with numbered source lines. Other supporting files are included in the complete HTML manual. Configured local passwords, if any, are explicitly omitted. Do not paste the display line numbers into code.

### S0099 insights-workspace.tsx

[apps/web/src/features/insights/insights-workspace.tsx](<D:/Projects new/Nexora/apps/web/src/features/insights/insights-workspace.tsx>)

SHA256 `ab76055a3b7d7eb6b8cfec090368b105e3a2bfb82c35a68794dbe1f22acc8e41`; 20 source lines; complete captured text.

```tsx
0001  "use client";
0002  import { EntityHeading } from "@/components/ui/entity-heading";
0003  import {useEffect,useState} from "react";
0004  import {moduleApi} from "@/features/shared/module-api";
0005  import { ColumnChooser } from "../shared/column-chooser";
0006  import { FilterViewToolbar } from "../shared/filter-view-toolbar";
0007  import { SortableHeader, parseTableSort } from "../shared/table-sort";
0008  import { ListingSearch, useListingColumns } from "../shared/entity-listing-controls";
0009  import {Panel,Field,inputClass,buttonClass} from "@/features/shared/module-ui";
0010  type Dataset={key:string;title:string;count:number};type Row={id:string;label:string;status:string;createdAtUtc:string};
0011  export function InsightsWorkspace({canExport}:{canExport:boolean}){
0012   const [datasets,setDatasets]=useState<Dataset[]>([]),[key,setKey]=useState(""),[rows,setRows]=useState<Row[]>([]),[total,setTotal]=useState(0),[error,setError]=useState(""),[busy,setBusy]=useState(false),[query,setQuery]=useState({search:"",status:"",fromUtc:null as string|null,toUtc:null as string|null,skip:0,take:50,sort:"-createdAtUtc"});
0013   const [filters,setFilters]=useState(query);
0014   const [search,setSearch]=useState("");
0015   const columns=useListingColumns(["Name","Status","Created"]);
0016   useEffect(()=>{moduleApi<{datasets:Dataset[]}>("insights","/dashboard").then(d=>{setDatasets(d.datasets);setKey(d.datasets[0]?.key??"");}).catch(e=>setError(e.message));},[]);
0017   async function run(q:typeof query){setBusy(true);setError("");try{const page=await moduleApi<{items:Row[];total:number}>("insights","/query/"+key,"POST",q);setRows(page.items);setTotal(page.total);setQuery(q);setFilters(q);}catch(e){setError(e instanceof Error?e.message:"Report failed");}finally{setBusy(false);}}
0018   async function exportReport(){setBusy(true);setError("");try{const csrf=await fetch("/api/v1/auth/csrf",{cache:"no-store"});if(!csrf.ok)throw new Error("Sign in again.");const {token}=await csrf.json();const response=await fetch("/api/v1/insights/export/"+key,{method:"POST",headers:{"Content-Type":"application/json","X-CSRF-TOKEN":token},body:JSON.stringify(query)});if(!response.ok){const p=await response.json();throw new Error(p.errors?Object.values(p.errors).flat().join(" "):p.title??"Export failed.");}const url=URL.createObjectURL(await response.blob());const a=document.createElement("a");a.href=url;a.download="nexora-"+key+".csv";a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);}catch(e){setError(e instanceof Error?e.message:"Export failed");}finally{setBusy(false);}}
0019   return <main className="space-y-6 p-6"><EntityHeading>Insights</EntityHeading><p className="text-slate-600">Explore records available to your tenant and role. Counts reflect the time this page loaded.</p>{error&&<p role="alert" className="text-red-700">{error}</p>}<div className="grid gap-4 sm:grid-cols-3">{datasets.map(d=><Panel key={d.key} title={d.title}><p className="text-3xl font-semibold">{d.count}</p></Panel>)}</div><FilterViewToolbar search={<ListingSearch value={search} onChange={setSearch} onSearch={()=>void run({...filters,search,skip:0})} disabled={busy||!key} />} onAllRecords={()=>{setSearch("");void run({...query,search:"",status:"",fromUtc:null,toUtc:null,skip:0});}} disabled={!key} actions={canExport&&<button disabled={busy||!key} className={buttonClass} onClick={()=>void exportReport()}>Export matching records</button>} filters={<div className="grid gap-3 sm:grid-cols-2"><Field label="Dataset"><select className={inputClass} value={key} onChange={e=>{setKey(e.target.value);setRows([]);setTotal(0);}}>{datasets.map(d=><option key={d.key} value={d.key}>{d.title}</option>)}</select></Field><Field label="Exact status"><input value={filters.status} maxLength={80} className={inputClass} onChange={e=>setFilters({...filters,status:e.target.value})}/></Field><Field label="Created on or after"><input type="date" value={filters.fromUtc?.slice(0,10)??""} className={inputClass} onChange={e=>setFilters({...filters,fromUtc:e.target.value?new Date(e.target.value).toISOString():null})}/></Field><Field label="Created before"><input type="date" value={filters.toUtc?.slice(0,10)??""} className={inputClass} onChange={e=>setFilters({...filters,toUtc:e.target.value?new Date(e.target.value).toISOString():null})}/></Field><button disabled={busy||!key} className={buttonClass} onClick={()=>void run({...filters,search,skip:0})}>Run report</button></div>} /><Panel title="Report results"><p role="status">{busy?"Loading…":`${total} matching records`}</p><div className="overflow-auto"><table className="w-full text-left text-sm"><thead><tr>{[["label","Name"],["status","Status"],["createdAtUtc","Created"]].map(([column,label])=><SortableHeader key={column} label={label} column={column} hidden={!columns.show(label)} sort={parseTableSort(query.sort)} disabled={busy||!key} onSort={sort=>void run({...query,sort,skip:0})}/>)}</tr></thead><tbody>{rows.map(r=><tr key={r.id} className="border-t"><td className="p-3">{r.label}</td><td hidden={!columns.show("Status")}>{r.status}</td><td hidden={!columns.show("Created")}>{new Date(r.createdAtUtc).toLocaleDateString()}</td></tr>)}</tbody></table></div><footer className="mt-4 flex flex-wrap items-center justify-between gap-3 border-t border-slate-200 pt-3"><ColumnChooser>{columns.controls}</ColumnChooser><div className="flex gap-3"><button disabled={busy||query.skip===0} className={buttonClass} onClick={()=>void run({...query,skip:Math.max(0,query.skip-50)})}>Previous</button><button disabled={busy||query.skip+50>=total} className={buttonClass} onClick={()=>void run({...query,skip:query.skip+50})}>Next</button></div></footer></Panel></main>;
0020  }
```

### S0578 ReportingEndpoints.cs

[backend/src/Nexora.Api/ReportingEndpoints.cs](<D:/Projects new/Nexora/backend/src/Nexora.Api/ReportingEndpoints.cs>)

SHA256 `efe06155ca5e7139ff041fac8c1233d5a797f2fca056d59649082e472d458bb1`; 20 source lines; complete captured text.

```cs
0001  using System.Text;
0002  using Nexora.BuildingBlocks.Endpoints;
0003  using Nexora.BuildingBlocks.Reporting;
0004  using Nexora.Modules.Identity.Security;
0005  using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
0006  namespace Nexora.Api;
0007  public static class ReportingEndpoints
0008  {
0009   public static void MapNexoraReporting(this WebApplication app)
0010   {
0011    var api=app.Module("insights");
0012    api.MapGet("/datasets",(IEnumerable<IReportDataset> datasets,HttpContext http)=>datasets.Where(d=>Allowed(http,d)).Select(d=>new{d.Key,d.Title,fields=new[]{"label","status","createdAtUtc"}}));
0013    api.MapGet("/dashboard",async(IEnumerable<IReportDataset> datasets,HttpContext http,CancellationToken ct)=>{var counts=new List<object>();foreach(var dataset in datasets.Where(d=>Allowed(http,d))){var page=await dataset.QueryAsync(new(Take:0),ct);counts.Add(new{dataset.Key,dataset.Title,count=page.Total});}return Results.Ok(new{asOfUtc=DateTime.UtcNow,datasets=counts});});
0014    api.MapPost("/query/{key}",async(string key,ReportQuery query,IEnumerable<IReportDataset> datasets,HttpContext http,CancellationToken ct)=>{Validate(query);var dataset=datasets.SingleOrDefault(d=>d.Key==key&&Allowed(http,d));if(dataset==null)return Results.NotFound();return Results.Ok(await dataset.QueryAsync(query,ct));});
0015    api.MapPost("/export/{key}",async(string key,ReportQuery query,IEnumerable<IReportDataset> datasets,HttpContext http,ILoggerFactory logs,CancellationToken ct)=>{Validate(query);var dataset=datasets.SingleOrDefault(d=>d.Key==key&&Allowed(http,d));if(dataset==null)return Results.NotFound();var page=await dataset.QueryAsync(query with{Skip=0,Take=10001},ct);Check(page.Total<=10000,"Narrow the filters to at most 10,000 records before export.");var csv=new StringBuilder("Id,Label,Status,CreatedUtc\r\n");foreach(var row in page.Items)csv.AppendLine(string.Join(",",row.Id,Csv(row.Label),Csv(row.Status),row.CreatedAtUtc.ToString("O")));logs.CreateLogger("Nexora.Reports").LogInformation("Report exported dataset={Dataset} rows={Rows} tenant={Tenant} actor={Actor} trace={Trace}",key,page.Items.Count,http.User.FindFirst("tenant_id")?.Value,http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,http.TraceIdentifier);return Results.File(Encoding.UTF8.GetBytes(csv.ToString()),"text/csv","nexora-"+key+".csv");}).RequireAuthorization("insights.export");
0016   }
0017   private static bool Allowed(HttpContext http,IReportDataset dataset)=>http.User.HasClaim(NexoraClaimTypes.Permission,dataset.Permission);
0018   private static void Validate(ReportQuery q){Choice(q.Sort,"label","-label","status","-status","createdAtUtc","-createdAtUtc");Check(q.Take is >0 and <=200&&q.Skip is >=0 and <=100000,"Use a page size of 1–200 and offset of 0–100000.");Check(q.Search==null||q.Search.Length<=160,"Search is too long.");Check(q.Status==null||q.Status.Length<=80,"Status is too long.");Check(q.FromUtc==null||q.ToUtc==null||q.FromUtc<q.ToUtc,"The end date must follow the start date.");}
0019   private static string Csv(string value){if(value.TrimStart().StartsWith('=')||value.TrimStart().StartsWith('+')||value.TrimStart().StartsWith('-')||value.TrimStart().StartsWith('@'))value="'"+value;return "\""+value.Replace("\"","\"\"")+"\"";}
0020  }
```

### S0200 ReportDataset.cs

[backend/src/BuildingBlocks/Nexora.BuildingBlocks/Reporting/ReportDataset.cs](<D:/Projects new/Nexora/backend/src/BuildingBlocks/Nexora.BuildingBlocks/Reporting/ReportDataset.cs>)

SHA256 `6616900a8a876256e4dcabd20398c39be5ae3e618f18effaa46ebc60cfde2826`; 32 source lines; complete captured text.

```cs
0001  using Microsoft.EntityFrameworkCore;
0002  namespace Nexora.BuildingBlocks.Reporting;
0003  public sealed record ReportQuery(string? Search=null,string? Status=null,DateTime? FromUtc=null,DateTime? ToUtc=null,int Skip=0,int Take=50,string Sort="-createdAtUtc");
0004  public sealed class ReportRow { public Guid Id {get;init;} public string Label {get;init;}=""; public string Status {get;init;}=""; public DateTime CreatedAtUtc {get;init;} }
0005  public sealed record ReportPage(int Total,IReadOnlyList<ReportRow> Items);
0006  public interface IReportDataset
0007  {
0008   string Key {get;} string Title {get;} string Permission {get;}
0009   Task<ReportPage> QueryAsync(ReportQuery query,CancellationToken ct);
0010   Task<ReportRow?> FindAsync(Guid id,CancellationToken ct);
0011  }
0012  public sealed class ReportDataset(string key,string title,string permission,IQueryable<ReportRow> source):IReportDataset
0013  {
0014   public Task<ReportRow?> FindAsync(Guid id,CancellationToken ct)=>source.SingleOrDefaultAsync(x=>x.Id==id,ct);
0015   public string Key=>key;public string Title=>title;public string Permission=>permission;
0016   public async Task<ReportPage> QueryAsync(ReportQuery request,CancellationToken ct)
0017   {
0018    var query=source;
0019    if(!string.IsNullOrWhiteSpace(request.Search))query=query.Where(x=>x.Label.Contains(request.Search));
0020    if(!string.IsNullOrWhiteSpace(request.Status))query=query.Where(x=>x.Status==request.Status);
0021    if(request.FromUtc!=null)query=query.Where(x=>x.CreatedAtUtc>=request.FromUtc);
0022    if(request.ToUtc!=null)query=query.Where(x=>x.CreatedAtUtc<request.ToUtc);
0023    var total=await query.CountAsync(ct);
0024    var ordered=request.Sort switch {
0025     "label"=>query.OrderBy(x=>x.Label), "-label"=>query.OrderByDescending(x=>x.Label),
0026     "status"=>query.OrderBy(x=>x.Status), "-status"=>query.OrderByDescending(x=>x.Status),
0027     "createdAtUtc"=>query.OrderBy(x=>x.CreatedAtUtc), _=>query.OrderByDescending(x=>x.CreatedAtUtc)
0028    };
0029    var rows=await ordered.ThenBy(x=>x.Id).Skip(request.Skip).Take(request.Take).ToListAsync(ct);
0030    return new(total,rows);
0031   }
0032  }
```
