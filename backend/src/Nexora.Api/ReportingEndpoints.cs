using System.Text;
using Nexora.BuildingBlocks.Endpoints;
using Nexora.BuildingBlocks.Reporting;
using Nexora.Modules.Identity.Security;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
namespace Nexora.Api;
public static class ReportingEndpoints
{
 public static void MapNexoraReporting(this WebApplication app)
 {
  var api=app.Module("insights");
  api.MapGet("/datasets",(IEnumerable<IReportDataset> datasets,HttpContext http)=>datasets.Where(d=>Allowed(http,d)).Select(d=>new{d.Key,d.Title,fields=new[]{"label","status","createdAtUtc"}}));
  api.MapGet("/dashboard",async(IEnumerable<IReportDataset> datasets,HttpContext http,CancellationToken ct)=>{var counts=new List<object>();foreach(var dataset in datasets.Where(d=>Allowed(http,d))){var page=await dataset.QueryAsync(new(Take:0),ct);counts.Add(new{dataset.Key,dataset.Title,count=page.Total});}return Results.Ok(new{asOfUtc=DateTime.UtcNow,datasets=counts});});
  api.MapPost("/query/{key}",async(string key,ReportQuery query,IEnumerable<IReportDataset> datasets,HttpContext http,CancellationToken ct)=>{Validate(query);var dataset=datasets.SingleOrDefault(d=>d.Key==key&&Allowed(http,d));if(dataset==null)return Results.NotFound();return Results.Ok(await dataset.QueryAsync(query,ct));});
  api.MapPost("/export/{key}",async(string key,ReportQuery query,IEnumerable<IReportDataset> datasets,HttpContext http,ILoggerFactory logs,CancellationToken ct)=>{Validate(query);var dataset=datasets.SingleOrDefault(d=>d.Key==key&&Allowed(http,d));if(dataset==null)return Results.NotFound();var page=await dataset.QueryAsync(query with{Skip=0,Take=10001},ct);Check(page.Total<=10000,"Narrow the filters to at most 10,000 records before export.");var csv=new StringBuilder("Id,Label,Status,CreatedUtc\r\n");foreach(var row in page.Items)csv.AppendLine(string.Join(",",row.Id,Csv(row.Label),Csv(row.Status),row.CreatedAtUtc.ToString("O")));logs.CreateLogger("Nexora.Reports").LogInformation("Report exported dataset={Dataset} rows={Rows} tenant={Tenant} actor={Actor} trace={Trace}",key,page.Items.Count,http.User.FindFirst("tenant_id")?.Value,http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,http.TraceIdentifier);return Results.File(Encoding.UTF8.GetBytes(csv.ToString()),"text/csv","nexora-"+key+".csv");}).RequireAuthorization("insights.export");
 }
 private static bool Allowed(HttpContext http,IReportDataset dataset)=>http.User.HasClaim(NexoraClaimTypes.Permission,dataset.Permission);
 private static void Validate(ReportQuery q){Check(q.Take is >0 and <=200&&q.Skip is >=0 and <=100000,"Use a page size of 1–200 and offset of 0–100000.");Check(q.Search==null||q.Search.Length<=160,"Search is too long.");Check(q.Status==null||q.Status.Length<=80,"Status is too long.");Check(q.FromUtc==null||q.ToUtc==null||q.FromUtc<q.ToUtc,"The end date must follow the start date.");}
 private static string Csv(string value){if(value.TrimStart().StartsWith('=')||value.TrimStart().StartsWith('+')||value.TrimStart().StartsWith('-')||value.TrimStart().StartsWith('@'))value="'"+value;return "\""+value.Replace("\"","\"\"")+"\"";}
}
