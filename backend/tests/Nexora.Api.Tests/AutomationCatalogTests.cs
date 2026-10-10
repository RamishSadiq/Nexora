using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Contracts;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed class AutomationCatalogTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;
    private sealed class FutureRecord
    {
        public Guid TenantId { get; set; }
        public Guid Version { get; set; }
        public long NewlyAddedNumber { get; set; }
        public DateOnly? NewlyAddedDate { get; set; }
        public bool NewlyAddedFlag { get; set; }
        [AutomationIgnore] public string PrivateValue { get; set; } = "";
        [System.ComponentModel.DataAnnotations.Schema.NotMapped] public string DisplayOnly => "";
    }

    [Fact]
    public void New_scalars_are_discovered_typed_and_normalized_without_allowlists()
    {
        var fields = Automations.BuiltIns(typeof(FutureRecord)).ToDictionary(field => field.Id);
        Assert.Equal("number", fields["record:NewlyAddedNumber"].DataType);
        Assert.Equal("date", fields["record:NewlyAddedDate"].DataType);
        Assert.Equal("boolean", fields["record:NewlyAddedFlag"].DataType);
        Assert.DoesNotContain("record:TenantId", fields.Keys);
        Assert.DoesNotContain("record:Version", fields.Keys);
        Assert.DoesNotContain("record:PrivateValue", fields.Keys);
        Assert.DoesNotContain("record:DisplayOnly", fields.Keys);
        Assert.Equal(42m, EntityFieldCatalog.RuleValue(42L));
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), EntityFieldCatalog.RuleValue(new DateOnly(2026, 1, 1)));
        var registration = new WorkflowEntityRegistration("future", "Future", "crm.read", typeof(FutureRecord), nameof(FutureRecord.NewlyAddedNumber), null, null, []);
        Assert.Equal(fields.Count + 1, registration.Metadata.Fields.Length);
        Assert.Equal("number", registration.Metadata.Fields.Single(field => field.Key == "newlyAddedNumber").Type);
    }

    [Fact]
    public async Task Old_layouts_reconcile_and_new_sections_and_profile_fields_are_available()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        const string path = "/api/v1/crm/automations/contact";
        var initial = await Json(await client.GetAsync(path));
        var available = initial["availableSections"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray();
        foreach (var name in new[] { "Contact details", "General Information", "Social Media Details", "Communication Permissions", "Profile", "Membership details", "Employers", "Connected Activity", "Data Audit" }) Assert.Contains(name, available);
        Assert.Equal(available.Length, initial["sectionDefinitions"]!.AsArray().Count);
        var fields = initial["fields"]!.AsArray().ToDictionary(node => node!["id"]!.GetValue<string>());
        Assert.Equal("text", fields["profile:FirstName"]!["dataType"]!.GetValue<string>());
        Assert.Equal("number", fields["profile:NumberOfFollowers"]!["dataType"]!.GetValue<string>());
        Assert.Equal("date", fields["profile:DateStopAllCommunication"]!["dataType"]!.GetValue<string>());
        Assert.Equal("boolean", fields["profile:StopAllCommunication"]!["dataType"]!.GetValue<string>());
        Assert.Contains("social:FacebookId", fields.Keys);
        Assert.All(fields.Values.Where(field => field!["id"]!.GetValue<string>().StartsWith("profile:")), field => Assert.False(field!["writable"]!.GetValue<bool>()));
        var saved = await Json(await Send(client, path, HttpMethod.Put, new { sections = new[] { "Notes", "Addresses" } }));
        var current = await Json(await client.GetAsync(path));
        Assert.Equal(new[] { "Notes", "Addresses" }, current["sections"]!.AsArray().Take(2).Select(node => node!.GetValue<string>()));
        Assert.Equal(available.Length, current["sections"]!.AsArray().Count);
        Assert.Equal(Version(saved), Version(current));
        Assert.Equal(new[] { "Notes", "Addresses", "New section" }, AutomationCatalog.Reconcile(["Notes", "Retired section", "Addresses", "Notes"], ["Addresses", "Notes", "New section"]));
        var account = await Json(await client.GetAsync("/api/v1/crm/automations/account"));
        Assert.Contains(account["fields"]!.AsArray(), field => field!["id"]!.GetValue<string>() == "profile:NumberOfEmployees" && field["dataType"]!.GetValue<string>() == "number");
        Assert.DoesNotContain(account["fields"]!.AsArray(), field => field!["id"]!.GetValue<string>() == "profile:FirstName");
    }

    [Fact]
    public async Task Profile_sources_execute_for_text_boolean_integer_date_and_social_changes()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        async Task<string> Field(string name, string type) => Id(await Json(await Send(client, "/api/v1/crm/fields", HttpMethod.Post, new { name, dataType = type, kind = "contact" })));
        var name = await Field("Copied first name", "text"); var number = await Field("Calculated followers", "number");
        var date = await Field("Permission date", "date"); var flag = await Field("Stopped flag", "boolean"); var social = await Field("Copied Facebook ID", "text");
        await Json(await Send(client, "/api/v1/crm/automations/contact", HttpMethod.Put, new { rules = new[] {
            new AutomationRule("profile:FirstName", "copy", name), new AutomationRule("profile:NumberOfFollowers", "add", number, "5"),
            new AutomationRule("profile:DateStopAllCommunication", "copy", date), new AutomationRule("profile:StopAllCommunication", "copy", flag),
            new AutomationRule("social:FacebookId", "copy", social)
        } }));
        var contact = await Contact(client); var path = "/api/v1/crm/records/" + Id(contact);
        await Json(await Send(client, path + "/profile", HttpMethod.Put, new {
            version = Version(contact), contactDetails = new ContactDetails(FirstName: "Amelia"),
            socialMediaDetails = new ContactSocialMediaDetails(FacebookId: "amelia-page", NumberOfFollowers: 42),
            communicationPermissions = new ContactCommunicationPermissions(StopAllCommunication: true, DateStopAllCommunication: new DateOnly(2026, 1, 1))
        }));
        var saved = await Json(await client.GetAsync(path));
        JsonNode Value(string id) => saved["fields"]!.AsArray().Single(row => row!["definitionId"]!.GetValue<string>() == id)!;
        Assert.Equal("Amelia", Value(name)["textValue"]!.GetValue<string>());
        Assert.Equal(47m, Value(number)["numberValue"]!.GetValue<decimal>());
        Assert.True(Value(flag)["booleanValue"]!.GetValue<bool>());
        Assert.StartsWith("2026-01-01", Value(date)["dateValue"]!.GetValue<string>());
        Assert.Equal("amelia-page", Value(social)["textValue"]!.GetValue<string>());
        // Remove a social child and clear its calculated output; unrelated profile values remain available.
        await Json(await Send(client, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), contactDetails = new ContactDetails(FirstName: "Amelia"), socialMediaDetails = new ContactSocialMediaDetails(NumberOfFollowers: 50) }));
        saved = await Json(await client.GetAsync(path));
        Assert.Null(Value(social)["textValue"]);
        Assert.Equal(55m, Value(number)["numberValue"]!.GetValue<decimal>());
        using var outside = await Login(app, "outside@nexora.test");
        Assert.Equal(HttpStatusCode.NotFound, (await outside.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Custom_attributes_appear_in_workflow_metadata_values_queries_and_change_events()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var definition = await Json(await Send(client, "/api/v1/crm/fields", HttpMethod.Post, new { name = "New score", dataType = "number", kind = "contact" }));
        var fieldId = Guid.Parse(Id(definition)); var key = "attribute_" + fieldId.ToString("N");
        var contact = await Json(await Send(client, "/api/v1/crm/records", HttpMethod.Post, new {
            kind = "contact", name = "Metadata contact", status = "active", attributes = new Dictionary<string, object> { [fieldId.ToString()] = new { numberValue = 42 } }
        }), HttpStatusCode.Created);
        var id = Guid.Parse(Id(contact));
        var metadata = await Json(await client.GetAsync("/api/v1/workflows/metadata"));
        var entity = metadata["entities"]!.AsArray().Single(row => row!["key"]!.GetValue<string>() == "contacts")!;
        Assert.Contains(entity["fields"]!.AsArray(), field => field!["key"]!.GetValue<string>() == key && field["type"]!.GetValue<string>() == "number");
        var profile = metadata["entities"]!.AsArray().Single(row => row!["key"]!.GetValue<string>() == "contact-profiles")!;
        Assert.Contains(profile["fields"]!.AsArray(), field => field!["key"]!.GetValue<string>() == "numberOfFollowers" && field["type"]!.GetValue<string>() == "number");
        var rows = await Json(await client.GetAsync("/api/v1/workflows/records/contacts"));
        Assert.Equal(42m, decimal.Parse(rows["items"]!.AsArray().Single(row => row!["id"]!.GetValue<string>() == id.ToString())!["values"]![key]!.GetValue<string>(), System.Globalization.CultureInfo.InvariantCulture));
        using var scope = app.Services.CreateScope();
        var original = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var created = await original.Set<WorkflowDomainEvent>().IgnoreQueryFilters().SingleAsync(row => row.RecordId == id && row.EntityKey == "contacts" && row.Kind == "record.created");
        Assert.Equal(42m, decimal.Parse(JsonNode.Parse(created.CurrentJson)![key]!.GetValue<string>(), System.Globalization.CultureInfo.InvariantCulture));
        var identity = new Identity(created.TenantId, created.ActorId);
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<CrmDbContext>>();
        await using var db = new CrmDbContext(options, identity);
        var registration = new WorkflowEntityRegistration("contacts", "Contacts", "crm.read", typeof(CrmRecord), "Name", "Kind", "contact", []);
        var source = new WorkflowEntitySource<CrmDbContext, CrmRecord>(db, registration, row => row.Kind == "contact");
        var matches = await source.QueryAsync(new(Field: key, Operator: "greater-than", Value: "40"), 10, default);
        Assert.Contains(matches.Items, row => row.Id == id);
        Assert.Empty((await source.QueryAsync(new(Field: key, Operator: "less-than", Value: "40"), 10, default)).Items);
        var value = await db.Set<CustomFieldValue>().SingleAsync(row => row.RecordId == id && row.DefinitionId == fieldId);
        value.NumberValue = 55;
        await db.SaveChangesAsync();
        var changed = await db.Set<WorkflowDomainEvent>().SingleAsync(row => row.RecordId == id && row.EntityKey == "contacts" && row.Kind == "record.updated");
        Assert.Equal(42m, decimal.Parse(JsonNode.Parse(changed.PreviousJson)![key]!.GetValue<string>(), System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(55m, decimal.Parse(JsonNode.Parse(changed.CurrentJson)![key]!.GetValue<string>(), System.Globalization.CultureInfo.InvariantCulture));
        await using var foreign = new CrmDbContext(options, new Identity(Guid.NewGuid(), Guid.NewGuid()));
        var foreignSource = new WorkflowEntitySource<CrmDbContext, CrmRecord>(foreign, registration, null);
        Assert.DoesNotContain(foreignSource.Metadata.Fields, field => field.Key == key);
        Assert.Null(await foreignSource.FindAsync(id, default));
    }
}
