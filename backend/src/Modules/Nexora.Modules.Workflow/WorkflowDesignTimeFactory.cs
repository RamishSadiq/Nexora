using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Workflow;
public sealed class WorkflowDesignTimeFactory:IDesignTimeDbContextFactory<WorkflowDbContext>
{
 private sealed record Identity(Guid? TenantId,Guid? UserId):IRequestIdentity;
 public WorkflowDbContext CreateDbContext(string[] args)=>new(new DbContextOptionsBuilder<WorkflowDbContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NexoraDesign;Trusted_Connection=True;TrustServerCertificate=True",sql=>sql.MigrationsHistoryTable("__EFMigrationsHistory","workflow")).Options,new Identity(null,null));
}
