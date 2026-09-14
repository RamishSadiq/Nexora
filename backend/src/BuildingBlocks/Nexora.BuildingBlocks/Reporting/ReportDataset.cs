using Microsoft.EntityFrameworkCore;
namespace Nexora.BuildingBlocks.Reporting;
public sealed record ReportQuery(string? Search=null,string? Status=null,DateTime? FromUtc=null,DateTime? ToUtc=null,int Skip=0,int Take=50);
public sealed class ReportRow { public Guid Id {get;init;} public string Label {get;init;}=""; public string Status {get;init;}=""; public DateTime CreatedAtUtc {get;init;} }
public sealed record ReportPage(int Total,IReadOnlyList<ReportRow> Items);
public interface IReportDataset
{
 string Key {get;} string Title {get;} string Permission {get;}
 Task<ReportPage> QueryAsync(ReportQuery query,CancellationToken ct);
}
public sealed class ReportDataset(string key,string title,string permission,IQueryable<ReportRow> source):IReportDataset
{
 public string Key=>key;public string Title=>title;public string Permission=>permission;
 public async Task<ReportPage> QueryAsync(ReportQuery request,CancellationToken ct)
 {
  var query=source;
  if(!string.IsNullOrWhiteSpace(request.Search))query=query.Where(x=>x.Label.Contains(request.Search));
  if(!string.IsNullOrWhiteSpace(request.Status))query=query.Where(x=>x.Status==request.Status);
  if(request.FromUtc!=null)query=query.Where(x=>x.CreatedAtUtc>=request.FromUtc);
  if(request.ToUtc!=null)query=query.Where(x=>x.CreatedAtUtc<request.ToUtc);
  var total=await query.CountAsync(ct);
  var rows=await query.OrderByDescending(x=>x.CreatedAtUtc).ThenBy(x=>x.Id).Skip(request.Skip).Take(request.Take).ToListAsync(ct);
  return new(total,rows);
 }
}
