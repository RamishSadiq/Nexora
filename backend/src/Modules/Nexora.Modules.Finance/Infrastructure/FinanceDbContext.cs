using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Nexora.BuildingBlocks.Domain;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Finance.Domain;
namespace Nexora.Modules.Finance.Infrastructure;
public sealed class FinanceDbContext(DbContextOptions<FinanceDbContext> options,IRequestIdentity identity):TenantDbContext(options,identity)
{
 protected override void OnModelCreating(ModelBuilder b)
 {
  b.HasDefaultSchema("finance");TenantTable<CatalogueProduct>(b,"Products");TenantTable<SalesOrder>(b,"Orders");TenantTable<OrderLine>(b,"OrderLines");TenantTable<PostedInvoice>(b,"Invoices");TenantTable<Receipt>(b,"Receipts");TenantTable<Credit>(b,"Credits");TenantTable<Allocation>(b,"Allocations");TenantTable<Refund>(b,"Refunds");TenantTable<FinanceHistory>(b,"History");
  b.Entity<OrderLine>().HasOne<SalesOrder>().WithMany().HasForeignKey(x=>new{x.TenantId,x.OrderId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<OrderLine>().HasOne<CatalogueProduct>().WithMany().HasForeignKey(x=>new{x.TenantId,x.ProductId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<PostedInvoice>().HasOne<SalesOrder>().WithMany().HasForeignKey(x=>new{x.TenantId,x.OrderId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Credit>().HasOne<PostedInvoice>().WithMany().HasForeignKey(x=>new{x.TenantId,x.InvoiceId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Allocation>().HasOne<PostedInvoice>().WithMany().HasForeignKey(x=>new{x.TenantId,x.InvoiceId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Allocation>().HasOne<Receipt>().WithMany().HasForeignKey(x=>new{x.TenantId,x.ReceiptId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Refund>().HasOne<Receipt>().WithMany().HasForeignKey(x=>new{x.TenantId,x.ReceiptId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Allocation>().HasOne<Allocation>().WithMany().HasForeignKey(x=>new{x.TenantId,x.ReversesId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<CatalogueProduct>().Property(x=>x.Sku).HasMaxLength(80);b.Entity<CatalogueProduct>().HasIndex(x=>new{x.TenantId,x.Sku}).IsUnique();
  b.Entity<PostedInvoice>().HasIndex(x=>new{x.TenantId,x.OrderId}).IsUnique();
  b.Entity<Receipt>().Property(x=>x.Reference).HasMaxLength(100);b.Entity<Receipt>().HasIndex(x=>new{x.TenantId,x.Reference}).IsUnique();
  b.Entity<Receipt>().Property(x=>x.StatementReference).HasMaxLength(100);b.Entity<Receipt>().HasIndex(x=>new{x.TenantId,x.StatementReference}).IsUnique().HasFilter("StatementReference IS NOT NULL");
  b.Entity<Credit>().Property(x=>x.Reference).HasMaxLength(100);b.Entity<Credit>().HasIndex(x=>new{x.TenantId,x.Reference}).IsUnique();
  b.Entity<Refund>().Property(x=>x.Reference).HasMaxLength(100);b.Entity<Refund>().HasIndex(x=>new{x.TenantId,x.Reference}).IsUnique();
  b.Entity<Allocation>().HasIndex(x=>new{x.TenantId,x.ReversesId}).IsUnique().HasFilter("ReversesId IS NOT NULL");ConfigureColumns(b);
 }
 protected override async Task ValidateEntityAsync(EntityEntry<TenantEntity> entry,CancellationToken ct)
 {
  if(entry.Entity is not (PostedInvoice or Receipt or SalesOrder)||entry.State==EntityState.Added)return;
  var stored=await entry.GetDatabaseValuesAsync(ct);if(stored==null)return;
  if(entry.State==EntityState.Deleted)throw new TenantBoundaryException();
  foreach(var p in entry.Properties.Where(x=>x.IsModified))
  {
   var allowed=p.Metadata.Name is "Version" or "UpdatedAtUtc"||(entry.Entity is Receipt&&p.Metadata.Name=="StatementReference")||(entry.Entity is SalesOrder&&p.Metadata.Name=="Status"&&Equals(stored["Status"],"draft"));
   if(!allowed&&!Equals(p.CurrentValue,stored[p.Metadata.Name]))throw new TenantBoundaryException();
  }
 }
}
