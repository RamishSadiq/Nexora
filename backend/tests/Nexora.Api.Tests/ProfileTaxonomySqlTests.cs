using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;

namespace Nexora.Api.Tests;

public sealed class ProfileTaxonomySqlTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;

    [SqlServerFact]
    public async Task Profile_taxonomy_upgrade_preserves_tags_and_enforces_scope_slots_and_foreign_keys()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER")) { InitialCatalog = "NexoraProfileTaxonomyTest_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlServer(connection.ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "crm")).Options;
        var identity = new Identity(Guid.NewGuid(), Guid.NewGuid()); await using var db = new CrmDbContext(options, identity);
        try
        {
            var migrations = db.Database.GetMigrations().ToArray(); var index = Array.FindIndex(migrations, x => x.EndsWith("_ProfileTaxonomy")); Assert.True(index > 0);
            await db.GetService<IMigrator>().MigrateAsync(migrations[index - 1]);
            var id = Guid.NewGuid(); var now = DateTime.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[Records] ([Id],[TenantId],[Kind],[Name],[Status],[IsArchived],[Version],[CreatedAtUtc],[UpdatedAtUtc]) VALUES ({id},{identity.TenantId},'contact','Existing contact','active',0,{Guid.NewGuid()},{now},{now})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[Tags] ([Id],[TenantId],[RecordId],[Name]) VALUES ({Guid.NewGuid()},{identity.TenantId},{id},'legacy')");
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); Assert.False(db.Database.HasPendingModelChanges());
            Assert.Equal("legacy", (await db.Set<RecordTag>().SingleAsync()).Name); Assert.Empty(await db.Set<ProfileDefinition>().ToListAsync()); Assert.Empty(await db.Set<RecordProfileAssignment>().ToListAsync());
            var a = new ProfileDefinition { TenantId = identity.TenantId!.Value, ParentNode = "Expertise", ChildNode = "Engineering" };
            var b = new ProfileDefinition { TenantId = identity.TenantId.Value, ParentNode = "Interest", ChildNode = "Engineering" };
            db.AddRange(a, b); await db.SaveChangesAsync();
            db.Add(new ProfileDefinition { TenantId = identity.TenantId.Value, ParentNode = "expertise", ChildNode = "ENGINEERING" }); await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            var assignment = new RecordProfileAssignment { TenantId = identity.TenantId.Value, RecordId = id, Slot = 5, DefinitionId = a.Id }; db.Add(assignment); await db.SaveChangesAsync();
            db.Add(new RecordProfileAssignment { TenantId = identity.TenantId.Value, RecordId = id, Slot = 1, DefinitionId = a.Id }); await Assert.ThrowsAsync<ValidationException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[ProfileAssignments] ([Id],[TenantId],[RecordId],[Slot],[DefinitionId]) VALUES ({Guid.NewGuid()},{identity.TenantId},{id},5,{b.Id})"));
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[ProfileAssignments] ([Id],[TenantId],[RecordId],[Slot],[DefinitionId]) VALUES ({Guid.NewGuid()},{identity.TenantId},{id},6,{b.Id})"));
            var outsider = new Identity(Guid.NewGuid(), Guid.NewGuid()); Guid foreignId;
            await using (var outside = new CrmDbContext(options, outsider))
            {
                Assert.Empty(await outside.Set<ProfileDefinition>().ToListAsync()); Assert.Empty(await outside.Set<RecordProfileAssignment>().ToListAsync());
                var foreign = new ProfileDefinition { TenantId = outsider.TenantId!.Value, ParentNode = "Expertise", ChildNode = "Engineering" }; outside.Add(foreign); await outside.SaveChangesAsync(); foreignId = foreign.Id;
            }
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[ProfileAssignments] ([Id],[TenantId],[RecordId],[Slot],[DefinitionId]) VALUES ({Guid.NewGuid()},{identity.TenantId},{id},1,{foreignId})"));
            db.Add(new RecordProfileAssignment { TenantId = identity.TenantId.Value, RecordId = id, Slot = 1, DefinitionId = foreignId }); await Assert.ThrowsAsync<CrmBoundaryException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            var other = new CrmRecord { TenantId = identity.TenantId.Value, Name = "Other" }; db.Add(other); await db.SaveChangesAsync();
            var stored = await db.Set<RecordProfileAssignment>().SingleAsync(); stored.RecordId = other.Id; await Assert.ThrowsAsync<CrmBoundaryException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            var definition = await db.Set<ProfileDefinition>().SingleAsync(x => x.Id == a.Id); definition.IsArchived = true; definition.Version = Guid.NewGuid(); await db.SaveChangesAsync();
            db.Add(new RecordProfileAssignment { TenantId = identity.TenantId.Value, RecordId = other.Id, Slot = 1, DefinitionId = a.Id }); await Assert.ThrowsAsync<CrmBoundaryException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [crm].[ProfileDefinitions] WHERE [Id] = {a.Id}"));
            db.Remove(await db.Set<ProfileDefinition>().SingleAsync(x => x.Id == b.Id)); await Assert.ThrowsAsync<CrmBoundaryException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            Assert.Equal(5, (await db.Set<RecordProfileAssignment>().SingleAsync()).Slot); Assert.Equal("legacy", (await db.Set<RecordTag>().SingleAsync()).Name);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
