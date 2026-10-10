using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Documents;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json;
namespace Nexora.Modules.Crm;
public static partial class CrmModule
{
    private static void MapRecordPresentation(RouteGroupBuilder api)
    {
        api.MapPost("/records/{id:guid}/photo",async(Guid id,CrmDbContext db,IRequestIdentity who,HttpContext http,IDocumentScanner scanner,CancellationToken ct)=>
        {
            var record=await RecordAsync(db,id,ct,true);
            var upload=await DocumentSafety.ReadAsync(http.Request,ct);
            var form=await http.Request.ReadFormAsync(ct);
            if(!Guid.TryParse(form["version"],out var version)||version!=record.Version)return Conflict();
            if(ImageType(upload.Content)==null)throw new ValidationException("Choose a PNG or JPEG image.");
            var scan=await scanner.ScanAsync(upload.Content,ct);
            if(scan!="clean")throw new ValidationException("The image must pass a security scan before it can be used.");
            var file=new RecordFile{TenantId=who.TenantId!.Value,RecordId=id,Name=upload.Name,Content=upload.Content,Sha256=Convert.ToHexString(SHA256.HashData(upload.Content)),ScanStatus=scan,ScannedAtUtc=DateTime.UtcNow};
            db.Add(file);record.PhotoFileId=file.Id;record.Version=Guid.NewGuid();record.UpdatedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync(ct);return Results.Ok(new{file.Id});
        }).RequireAuthorization("crm.manage","crm.files");
        api.MapGet("/records/{id:guid}/photo",async(Guid id,CrmDbContext db,HttpContext http,CancellationToken ct)=>
        {
            var record=await RecordAsync(db,id,ct);
            var file=await db.Set<RecordFile>().SingleOrDefaultAsync(f=>f.Id==record.PhotoFileId&&f.RecordId==id,ct);
            if(file==null||file.ScanStatus!="clean"||file.Sha256!=Convert.ToHexString(SHA256.HashData(file.Content))||ImageType(file.Content) is not string contentType)return Results.NotFound();
            http.Response.Headers.CacheControl="private, no-store";http.Response.Headers.XContentTypeOptions="nosniff";
            return Results.File(file.Content,contentType);
        }).RequireAuthorization("crm.files");
        api.MapGet("/records/{id:guid}/data-audit",async(Guid id,DateTime? fromUtc,DateTime? toUtc,int? page,CrmDbContext db,CancellationToken ct)=>
        {
            await RecordAsync(db,id,ct);
            if(fromUtc>toUtc)throw new ValidationException("The end date must follow the start date.");
            var number=Math.Max(1,page??1);if(number>10000)throw new ValidationException("Page is out of range.");
            IQueryable<AuditRow>? query=null;
            foreach(var type in db.Model.GetEntityTypes().Where(t=>typeof(DataAuditEntry).IsAssignableFrom(t.ClrType)))
            {
                // CRM audit is scoped to this record and its children; saved views/configuration are never included.
                var source=type.ClrType.GenericTypeArguments[0];
                if(source!=typeof(CrmRecord)&&!typeof(RecordChild).IsAssignableFrom(source)&&source!=typeof(AccountContact)&&source!=typeof(AccountContactRole))continue;
                var method=typeof(CrmModule).GetMethod(nameof(QueryAudit),System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.MakeGenericMethod(source);
                var part=(IQueryable<AuditRow>)method.Invoke(null,[db,id])!;
                query=query==null?part:query.Concat(part);
            }
            query ??= QueryAudit<CrmRecord>(db,id);
            var latest=await query.MaxAsync(r=>(DateTime?)r.ChangedAtUtc,ct);
            if(fromUtc!=null)query=query.Where(r=>r.ChangedAtUtc>=fromUtc);if(toUtc!=null)query=query.Where(r=>r.ChangedAtUtc<toUtc);
            var total=await query.CountAsync(ct);
            var rows=await query.OrderByDescending(r=>r.ChangedAtUtc).ThenBy(r=>r.Id).Skip((number-1)*20).Take(20).ToListAsync(ct);
            return Results.Ok(new{total,page=number,pageSize=20,latestAtUtc=latest,items=rows.Select(r=>new{r.Id,r.EntityId,r.Operation,r.ChangedAtUtc,entity=r.Entity,changes=JsonSerializer.Deserialize<DataAuditChange[]>(r.ChangesJson)})});
        });
    }
    private sealed class AuditRow
    {
        public Guid Id {get;set;}
        public string EntityId {get;set;}="";
        public string Entity {get;set;}="";
        public string Operation {get;set;}="";
        public DateTime ChangedAtUtc {get;set;}
        public string ChangesJson {get;set;}="";
    }
    private static IQueryable<AuditRow> QueryAudit<T>(CrmDbContext db,Guid id) where T:class =>
        db.Set<DataAudit<T>>().AsNoTracking().Where(a=>a.EntityId==id.ToString()||a.ParentId==id||a.OtherParentId==id)
            .Select(a=>new AuditRow{Id=a.Id,EntityId=a.EntityId,Entity=typeof(T).Name,Operation=a.Operation,ChangedAtUtc=a.ChangedAtUtc,ChangesJson=a.ChangesJson});
    private static string? ImageType(byte[] bytes)=>bytes.Length>=8&&bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})?"image/png":bytes.Length>=3&&bytes[0]==255&&bytes[1]==216&&bytes[2]==255?"image/jpeg":null;
}
