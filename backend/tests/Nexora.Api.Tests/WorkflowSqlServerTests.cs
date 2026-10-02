using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Workflow;
namespace Nexora.Api.Tests;
public sealed class WorkflowSqlServerTests
{
 private sealed record Identity(Guid? TenantId,Guid? UserId):IRequestIdentity;
 [SqlServerFact]
 public async Task Migrations_replay_and_two_workers_claim_one_lease_on_sql_server()
 {
  var connection=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER")){InitialCatalog="NexoraWorkflowTest_"+Guid.NewGuid().ToString("N")};
  var options=new DbContextOptionsBuilder<WorkflowDbContext>().UseSqlServer(connection.ConnectionString,sql=>sql.MigrationsHistoryTable("__EFMigrationsHistory","workflow")).Options;
  var identity=new Identity(Guid.NewGuid(),Guid.NewGuid());await using var db=new WorkflowDbContext(options,identity);
  try
  {
   await db.Database.MigrateAsync();await db.Database.MigrateAsync();Assert.False(db.Database.HasPendingModelChanges());Assert.Empty(await db.Database.GetPendingMigrationsAsync());
   var definition=new WorkflowDefinition{TenantId=identity.TenantId!.Value,Name="Lease",DraftJson="{}"};var version=new WorkflowVersion{TenantId=definition.TenantId,DefinitionId=definition.Id,Number=1,PublishedBy=identity.UserId!.Value,GraphJson="{}"};var run=new WorkflowRun{TenantId=definition.TenantId,WorkflowVersionId=version.Id,ActorId=identity.UserId.Value,RecordId=Guid.NewGuid(),RequestKey="lease",CurrentNodeId="end"};db.AddRange(definition,version,run);await db.SaveChangesAsync();
   var now=DateTime.UtcNow;
   async Task<int> Claim(){await using var contender=new WorkflowDbContext(options,identity);return await contender.Set<WorkflowRun>().Where(x=>x.Id==run.Id&&(x.LeaseUntilUtc==null||x.LeaseUntilUtc<now)).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.LeaseUntilUtc,now.AddMinutes(3)));}
   var results=await Task.WhenAll(Claim(),Claim());Assert.Equal(1,results.Sum());
   await db.Set<WorkflowRun>().Where(x=>x.Id==run.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.LeaseUntilUtc,now.AddMinutes(-1)));Assert.Equal(1,await Claim());
   await using var outsider=new WorkflowDbContext(options,new Identity(Guid.NewGuid(),Guid.NewGuid()));Assert.Empty(await outsider.Set<WorkflowRun>().ToListAsync());
  }
  finally{await db.Database.EnsureDeletedAsync();}
 }
}
