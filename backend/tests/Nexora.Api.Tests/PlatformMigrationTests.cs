using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using Nexora.Modules.Crm.Infrastructure;
using Nexora.Modules.Membership.Infrastructure;
using Nexora.Modules.Events.Infrastructure;
using Nexora.Modules.Finance.Infrastructure;
using Nexora.Modules.Engagement.Infrastructure;
using Nexora.Modules.Work.Infrastructure;
namespace Nexora.Api.Tests;
public sealed class PlatformMigrationTests
{
 private sealed record Identity(Guid? TenantId,Guid? UserId):IRequestIdentity;
 [SqlServerFact]
 public async Task All_module_migrations_share_one_catalog_and_replay_without_pending_changes()
 {
  var connection=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER")){InitialCatalog="NexoraPlatformTest_"+Guid.NewGuid().ToString("N")};
  var identity=new Identity(Guid.NewGuid(),Guid.NewGuid());
  DbContextOptions<T> Options<T>(string? schema=null) where T:DbContext=>new DbContextOptionsBuilder<T>().UseSqlServer(connection.ConnectionString,sql=>{if(schema!=null)sql.MigrationsHistoryTable("__EFMigrationsHistory",schema);}).Options;
  DbContext[] contexts=[new NexoraIdentityDbContext(Options<NexoraIdentityDbContext>()),new CrmDbContext(Options<CrmDbContext>("crm"),identity),new MembershipDbContext(Options<MembershipDbContext>("membership"),identity),new EventsDbContext(Options<EventsDbContext>("events"),identity),new FinanceDbContext(Options<FinanceDbContext>("finance"),identity),new EngagementDbContext(Options<EngagementDbContext>("engagement"),identity),new WorkDbContext(Options<WorkDbContext>("work"),identity)];
  try{foreach(var db in contexts)await db.Database.MigrateAsync();foreach(var db in contexts){await db.Database.MigrateAsync();Assert.Empty(await db.Database.GetPendingMigrationsAsync());Assert.False(db.Database.HasPendingModelChanges());}}
  finally{await contexts[0].Database.EnsureDeletedAsync();foreach(var db in contexts)await db.DisposeAsync();}
 }
}
