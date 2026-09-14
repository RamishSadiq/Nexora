using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Events.Domain;
namespace Nexora.Modules.Events.Infrastructure;
public sealed class EventsDbContext(DbContextOptions<EventsDbContext> options,IRequestIdentity identity):TenantDbContext(options,identity)
{
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("events");TenantTable<Offering>(b,"Offerings");TenantTable<EventSession>(b,"Sessions");TenantTable<Enrollment>(b,"Enrollments");TenantTable<ExamResult>(b,"Results");TenantTable<EventHistory>(b,"History");
        b.Entity<EventSession>().HasOne<Offering>().WithMany().HasForeignKey(x=>new{x.TenantId,x.OfferingId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Enrollment>().HasOne<Offering>().WithMany().HasForeignKey(x=>new{x.TenantId,x.OfferingId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ExamResult>().HasOne<Enrollment>().WithMany().HasForeignKey(x=>new{x.TenantId,x.EnrollmentId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EventHistory>().HasOne<Offering>().WithMany().HasForeignKey(x=>new{x.TenantId,x.OfferingId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Enrollment>().HasIndex(x=>new{x.TenantId,x.OfferingId,x.ContactId}).IsUnique();
        b.Entity<ExamResult>().HasIndex(x=>new{x.TenantId,x.EnrollmentId}).IsUnique();
        b.Entity<Offering>().HasIndex(x=>new{x.TenantId,x.StartsAtUtc});ConfigureColumns(b);
    }
}
