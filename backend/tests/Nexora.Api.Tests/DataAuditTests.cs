using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;
namespace Nexora.Api.Tests;
public sealed class DataAuditTests
{
 private sealed record Identity(Guid? TenantId,Guid? UserId):IRequestIdentity;
 [Fact]
 public async Task Field_history_is_atomic_tenant_scoped_and_immutable()
 {
  await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
  var options=new DbContextOptionsBuilder<CrmDbContext>().UseSqlite(connection).Options;
  var identity=new Identity(Guid.NewGuid(),Guid.NewGuid());
  await using var db=new CrmDbContext(options,identity);await db.Database.EnsureCreatedAsync();
  var record=new CrmRecord{TenantId=identity.TenantId!.Value,Name="Before",Kind="contact"};db.Add(record);await db.SaveChangesAsync();
  record.Name="After";await db.SaveChangesAsync();
  var audits=await db.Set<DataAudit<CrmRecord>>().OrderBy(x=>x.ChangedAtUtc).ToListAsync();Assert.Equal(2,audits.Count);Assert.Contains("Before",audits[1].ChangesJson);Assert.Contains("After",audits[1].ChangesJson);
  await using(var outside=new CrmDbContext(options,new Identity(Guid.NewGuid(),Guid.NewGuid())))Assert.Empty(await outside.Set<DataAudit<CrmRecord>>().ToListAsync());
  await using(var missing=new CrmDbContext(options,new Identity(null,null)))Assert.Empty(await missing.Set<DataAudit<CrmRecord>>().ToListAsync());
  await using(var tx=await db.Database.BeginTransactionAsync()){record.Name="Rolled back";await db.SaveChangesAsync();await tx.RollbackAsync();}
  db.ChangeTracker.Clear();Assert.Equal(2,await db.Set<DataAudit<CrmRecord>>().CountAsync());Assert.Equal("After",(await db.Records.SingleAsync()).Name);
  var audit=await db.Set<DataAudit<CrmRecord>>().FirstAsync();db.Remove(audit);await Assert.ThrowsAsync<InvalidOperationException>(()=>db.SaveChangesAsync());
 }
 [Fact]
 public async Task Binary_content_is_redacted_and_child_changes_keep_record_parent()
 {
  await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();var identity=new Identity(Guid.NewGuid(),Guid.NewGuid());
  await using var db=new CrmDbContext(new DbContextOptionsBuilder<CrmDbContext>().UseSqlite(connection).Options,identity);await db.Database.EnsureCreatedAsync();
  var record=new CrmRecord{TenantId=identity.TenantId!.Value,Name="Photo"};db.Add(record);await db.SaveChangesAsync();
  db.Add(new RecordFile{TenantId=record.TenantId,RecordId=record.Id,Name="photo.png",Content=[1,2,3],Sha256="private hash"});await db.SaveChangesAsync();
  var audit=await db.Set<DataAudit<RecordFile>>().SingleAsync();Assert.Equal(record.Id,audit.ParentId);Assert.Contains("[redacted]",audit.ChangesJson);Assert.DoesNotContain("private hash",audit.ChangesJson);
 }
}
