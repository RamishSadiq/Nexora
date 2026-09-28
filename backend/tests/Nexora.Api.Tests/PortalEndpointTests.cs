using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using Nexora.Modules.Identity.Security;
using Nexora.Modules.Identity;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;
public sealed class PortalEndpointTests
{
    private sealed class ResultScanner(string result) : Nexora.BuildingBlocks.Documents.IDocumentScanner
    {
        public Task<string> ScanAsync(byte[] content, CancellationToken ct) => Task.FromResult(result);
    }
    private static async Task<JsonNode> UploadEvidence(HttpClient member, string path, string version)
    {
        var csrf = await member.GetFromJsonAsync<CsrfResponse>("/api/v1/portal/csrf");
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(version), "version");
        form.Add(new ByteArrayContent("proof"u8.ToArray()), "file", "proof.txt");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/portal" + path) { Content = form };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        return await Json(await member.SendAsync(request));
    }

    [Fact]
    public async Task Rejected_evidence_is_unavailable_and_notification_explains_rejection()
    {
        using var app = new IdentityApiFactory(new ResultScanner("rejected")); using var staff = await Login(app); using var member = app.CreateClient();
        await Grant(app, staff); await PortalLogin(member);
        var product = await Json(await Send(staff, "/api/v1/membership/products", HttpMethod.Post, new { name = "Rejected evidence", category = "individual", termMonths = 12, rate = 0, currency = "GBP", isActive = true }));
        await Json(await Send(staff, $"/api/v1/membership/products/{Id(product)}/portal", HttpMethod.Put, new { version = Version(product), enabled = true, terms = "Terms" }));
        var id = Guid.NewGuid();
        var draft = await Json(await PortalSend(member, $"/drafts/{id}", HttpMethod.Put, new { productId = Id(product), startsAtUtc = DateTime.UtcNow.Date, notes = "", version = (string?)null }));
        var uploaded = await UploadEvidence(member, $"/drafts/{id}/evidence", Version(draft));
        Assert.Equal("rejected", uploaded["document"]!["scanStatus"]!.GetValue<string>());
        var fileId = uploaded["document"]!["id"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.BadRequest, (await member.GetAsync($"/api/v1/portal/drafts/{id}/evidence/{fileId}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, $"/drafts/{id}/evidence/{fileId}/scan", HttpMethod.Post, new { version = uploaded["draftVersion"]!.GetValue<string>() })).StatusCode);
        Assert.Contains("Evidence file rejected", (await Json(await member.GetAsync("/api/v1/portal/notifications"))).ToJsonString());
    }

    [Fact]
    public async Task Postal_address_is_shared_allowlisted_and_versioned_without_replacing_other_addresses()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        var (_, contact) = await Grant(app, staff); await PortalLogin(member);
        var profile = await Json(await member.GetAsync("/api/v1/portal/profile"));
        await Json(await Send(staff, $"/api/v1/crm/records/{Id(contact)}/addresses", HttpMethod.Post, new { label = "Office", line1 = "Staff address", line2 = "", city = "London", region = "", postalCode = "SW1A 1AA", country = "UK" }));
        profile = await Json(await member.GetAsync("/api/v1/portal/profile"));
        var address = new { line1 = "12 Member Road", line2 = "", city = "London", region = "", postalCode = "SW1A 1AA", country = "United Kingdom" };
        var updated = await Json(await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Member", version = Version(profile), postalAddress = address }));
        Assert.Equal("12 Member Road", updated["postalAddress"]!["line1"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict, (await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Stale", version = Version(profile), postalAddress = (object?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Unsafe", version = Version(updated), postalAddress = new { line1 = "Injected", contactId = Guid.NewGuid() } })).StatusCode);
        var renamed = await Json(await PortalSend(member, "/profile", HttpMethod.Put, new { name = "New name", version = Version(updated) }));
        Assert.Equal("London", renamed["postalAddress"]!["city"]!.GetValue<string>());
        var cleared = await Json(await PortalSend(member, "/profile", HttpMethod.Put, new { name = "New name", version = Version(renamed), postalAddress = (object?)null }));
        Assert.Null(cleared["postalAddress"]);
        var shared = await Json(await staff.GetAsync($"/api/v1/crm/records/{Id(contact)}"));
        Assert.Contains("Staff address", shared.ToJsonString());
        Assert.DoesNotContain("12 Member Road", shared.ToJsonString());
    }

    [Fact]
    public async Task Organisation_grants_are_explicit_scoped_revocable_and_do_not_expose_personal_records()
    {
        using var app = new IdentityApiFactory(new ResultScanner("clean")); using var staff = await Login(app); using var member = app.CreateClient();
        var (access, contact) = await Grant(app, staff); await PortalLogin(member);
        var account = await Json(await Send(staff, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Company A", status = "active" }), HttpStatusCode.Created);
        var path = $"/organisations/{Id(account)}";
        var adminPath = $"/api/v1/admin/portal-access/{access}/organisations";
        Assert.Equal(HttpStatusCode.NotFound, (await Send(staff, adminPath, HttpMethod.Post, new { accountId = Id(account), canSubmitApplications = true })).StatusCode);
        var link = await Json(await Send(staff, $"/api/v1/crm/accounts/{Id(account)}/contacts", HttpMethod.Post, new { contactId = Id(contact), startDate = DateTime.UtcNow.Date.AddDays(-1), jobTitle = "Director", workClassification = "technical", isMainContact = true, isAuthorisedContact = true, roles = new[] { "membership" } }));
        Assert.Empty((await Json(await member.GetAsync("/api/v1/portal/organisations"))).AsArray());
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/v1/portal{path}/memberships")).StatusCode);
        var viewGrant = await Json(await Send(staff, adminPath, HttpMethod.Post, new { accountId = Id(account), canSubmitApplications = false }));
        Assert.Single((await Json(await member.GetAsync("/api/v1/portal/organisations"))).AsArray());
        await Json(await member.GetAsync($"/api/v1/portal{path}/memberships"));
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync($"/api/v1/portal{path}/drafts")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, adminPath, HttpMethod.Post, new { accountId = Id(account), canSubmitApplications = true })).StatusCode);
        await Send(staff, $"{adminPath}/{Id(viewGrant)}", HttpMethod.Delete);
        var grant = await Json(await Send(staff, adminPath, HttpMethod.Post, new { accountId = Id(account), canSubmitApplications = true }));
        var product = await Json(await Send(staff, "/api/v1/membership/products", HttpMethod.Post, new { name = "Company product", category = "corporate", termMonths = 12, rate = 0, currency = "GBP", isActive = true }));
        await Json(await Send(staff, $"/api/v1/membership/products/{Id(product)}/portal", HttpMethod.Put, new { version = Version(product), enabled = false, organisationEnabled = true, terms = "Company terms" }));
        var draftId = Guid.NewGuid();
        var draft = await Json(await PortalSend(member, $"{path}/drafts/{draftId}", HttpMethod.Put, new { version = (string?)null, productId = Id(product), startsAtUtc = DateTime.UtcNow.Date, notes = "Representative notes" }));
        var uploaded = await UploadEvidence(member, $"{path}/drafts/{draftId}/evidence", Version(draft));
        var fileId = uploaded["document"]!["id"]!.GetValue<string>();
        draft["version"] = uploaded["draftVersion"]!.GetValue<string>();
        Assert.Empty((await Json(await member.GetAsync("/api/v1/portal/drafts"))).AsArray());
        Assert.Equal(HttpStatusCode.NotFound, (await PortalSend(member, $"/drafts/{draftId}/submit", HttpMethod.Post, new { version = Version(draft), termsAccepted = true })).StatusCode);
        var submitted = await Json(await PortalSend(member, $"{path}/drafts/{draftId}/submit", HttpMethod.Post, new { version = Version(draft), termsAccepted = true }));
        var id = submitted["applicationId"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/v1/portal/applications/{id}")).StatusCode);
        var receipt = await Json(await member.GetAsync($"/api/v1/portal{path}/applications/{id}"));
        Assert.Equal("Company terms", receipt["acceptedTerms"]!.GetValue<string>());
        Assert.Equal("proof", await (await member.GetAsync($"/api/v1/portal{path}/applications/{id}/evidence/{fileId}")).Content.ReadAsStringAsync());
        var staffFile = await staff.GetAsync($"/api/v1/membership/applications/{id}/evidence/{fileId}");
        Assert.Equal(HttpStatusCode.OK, staffFile.StatusCode);
        Assert.Equal("nosniff", staffFile.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/v1/portal/applications/{id}/evidence/{fileId}")).StatusCode);

        Assert.Empty((await Json(await member.GetAsync("/api/v1/portal/memberships")))["applications"]!.AsArray());
        var application = (await Json(await staff.GetAsync("/api/v1/membership/applications")))["items"]![0]!;
        await Json(await Send(staff, $"/api/v1/membership/applications/{id}/decision", HttpMethod.Post, new { version = Version(application), decision = "approve", reason = "PRIVATE decision" }));
        receipt = await Json(await member.GetAsync($"/api/v1/portal{path}/applications/{id}"));
        Assert.Equal("active", receipt["membership"]!["status"]!.GetValue<string>());
        Assert.DoesNotContain("PRIVATE", receipt.ToJsonString());
        Assert.Single((await Json(await member.GetAsync($"/api/v1/portal{path}/memberships")))["memberships"]!.AsArray());
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/v1/portal/organisations/{Guid.NewGuid()}/applications/{id}")).StatusCode);
        await Send(staff, $"{adminPath}/{Id(grant)}", HttpMethod.Delete);
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/v1/portal{path}/applications/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/v1/portal{path}/applications/{id}/evidence/{fileId}")).StatusCode);

        await Json(await Send(staff, adminPath, HttpMethod.Post, new { accountId = Id(account), canSubmitApplications = true }));
        await Json(await Send(staff, $"/api/v1/crm/account-contacts/{Id(link)}/end", HttpMethod.Post, new { version = Version(link), endDate = DateTime.UtcNow.Date, reason = "Left company" }));
        Assert.Empty((await Json(await member.GetAsync("/api/v1/portal/organisations"))).AsArray());
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/v1/portal{path}/memberships")).StatusCode);
    }

    [Fact]
    public async Task Staff_access_list_is_paged_tenant_scoped_and_unavailable_to_members()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app);
        var (ownGrant, _) = await Grant(app, staff);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>();
            using var system = db.BeginSystemAccess();
            var role = await db.Roles.SingleAsync(r => r.Name == "Outside Administrator");
            db.RolePermissions.AddRange(new Nexora.Modules.Identity.Domain.RolePermission { RoleId = role.Id, Permission = "identity.manage" },
                new Nexora.Modules.Identity.Domain.RolePermission { RoleId = role.Id, Permission = "crm.manage" });
            await db.SaveChangesAsync();
        }
        using var outside = await Login(app, "outside@nexora.test");
        var (otherGrant, _) = await Grant(app, outside);
        var response = await staff.GetAsync("/api/v1/admin/portal-access");
        var list = await Json(response);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        Assert.Equal(1, list["totalCount"]!.GetValue<int>());
        Assert.Equal(ownGrant, list["items"]![0]!["id"]!.GetValue<string>());
        Assert.DoesNotContain(otherGrant, list.ToJsonString());
        Assert.DoesNotContain("password", list.ToJsonString().ToLowerInvariant());
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.GetAsync("/api/v1/admin/portal-access?page=invalid")).StatusCode);
        using var member = app.CreateClient(); await PortalLogin(member);
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/v1/admin/portal-access")).StatusCode);
        await Send(staff, $"/api/v1/admin/portal-access/{ownGrant}", HttpMethod.Delete);
        list = await Json(await staff.GetAsync("/api/v1/admin/portal-access"));
        Assert.True(list["items"]![0]!["isRevoked"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Receipts_preserve_accepted_snapshot_and_show_safe_decision_without_staff_notes()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        await Grant(app, staff); await PortalLogin(member);
        var product = await Json(await Send(staff, "/api/v1/membership/products", HttpMethod.Post, new { name = "Receipt product", category = "individual", termMonths = 12, rate = 0, currency = "GBP", isActive = true }));
        product = await Json(await Send(staff, $"/api/v1/membership/products/{Id(product)}/portal", HttpMethod.Put, new { version = Version(product), enabled = true, terms = "Accepted original terms" }));
        var draftId = Guid.NewGuid();
        var draft = await Json(await PortalSend(member, $"/drafts/{draftId}", HttpMethod.Put, new { productId = Id(product), startsAtUtc = DateTime.UtcNow.Date, notes = "My submitted notes", version = (string?)null }));
        var submit = await Json(await PortalSend(member, $"/drafts/{draftId}/submit", HttpMethod.Post, new { version = Version(draft), termsAccepted = true }));
        var applicationId = submit["applicationId"]!.GetValue<string>();
        var notifications = await Json(await member.GetAsync("/api/v1/portal/notifications"));
        Assert.Contains(notifications.AsArray(), n => n!["kind"]!.GetValue<string>() == "application.submitted" && n["applicationId"]!.GetValue<string>() == applicationId && n["deliveryStatus"]!.GetValue<string>() == "queued");
        var notification = notifications.AsArray().First(n => n!["applicationId"]!.GetValue<string>() == applicationId)!;
        var notificationId = notification["id"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.NoContent, (await PortalSend(member, $"/notifications/{notificationId}/read", HttpMethod.Post)).StatusCode);
        Assert.True((await Json(await member.GetAsync("/api/v1/portal/notifications"))).AsArray().First(n => n!["id"]!.GetValue<string>() == notificationId)!["isRead"]!.GetValue<bool>());
        var receiptResponse = await member.GetAsync($"/api/v1/portal/applications/{applicationId}");
        Assert.Equal("no-store", receiptResponse.Headers.CacheControl!.ToString());
        var receipt = await Json(receiptResponse);
        Assert.Equal("submitted", receipt["status"]!.GetValue<string>());
        Assert.Null(receipt["membership"]);
        Assert.Equal("Accepted original terms", receipt["acceptedTerms"]!.GetValue<string>());
        Assert.Equal("My submitted notes", receipt["submittedNotes"]!.GetValue<string>());
        Assert.NotNull(receipt["termsAcceptedAtUtc"]);
        Assert.Equal(HttpStatusCode.Unauthorized, (await staff.GetAsync($"/api/v1/portal/applications/{applicationId}")).StatusCode);
        using var other = app.CreateClient(); var otherContact = await Contact(staff);
        await Json(await Send(staff, "/api/v1/admin/portal-access/local-member", HttpMethod.Post, new { email = "receipt.other@nexora.test", password = IdentityApiFactory.AdminPassword, contactId = Id(otherContact) }));
        await PortalSend(other, "/login", HttpMethod.Post, new LoginRequest("receipt.other@nexora.test", IdentityApiFactory.AdminPassword, false));
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/portal/applications/{applicationId}")).StatusCode);
        Assert.DoesNotContain((await Json(await other.GetAsync("/api/v1/portal/notifications"))).AsArray(), n => n!["id"]!.GetValue<string>() == notificationId);
        Assert.Equal(HttpStatusCode.NotFound, (await PortalSend(other, $"/notifications/{notificationId}/read", HttpMethod.Post)).StatusCode);

        await Send(staff, $"/api/v1/membership/products/{Id(product)}/portal", HttpMethod.Put, new { version = Version(product), enabled = true, terms = "Changed after submission" });
        var application = (await Json(await staff.GetAsync("/api/v1/membership/applications")))["items"]![0]!;
        await Json(await Send(staff, $"/api/v1/membership/applications/{applicationId}/decision", HttpMethod.Post, new { version = Version(application), decision = "approve", reason = "PRIVATE STAFF DECISION" }));
        receipt = await Json(await member.GetAsync($"/api/v1/portal/applications/{applicationId}"));
        Assert.Equal("approved", receipt["status"]!.GetValue<string>());
        Assert.Equal("active", receipt["membership"]!["status"]!.GetValue<string>());
        Assert.Equal("Accepted original terms", receipt["acceptedTerms"]!.GetValue<string>());
        Assert.DoesNotContain("PRIVATE", receipt.ToJsonString());
        Assert.Null(receipt["contactId"]); Assert.Null(receipt["tenantId"]); Assert.Null(receipt["decisionReason"]);
        var stored = (await Json(await member.GetAsync("/api/v1/portal/drafts"))).AsArray();
        Assert.Empty(stored);
        // A submitted snapshot is retained, even if its current version is known.
        using var scope = app.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<Nexora.Modules.Membership.Infrastructure.MembershipDbContext>>();
        var session = (await staff.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;
        using var db = new Nexora.Modules.Membership.Infrastructure.MembershipDbContext(options, new MemberIdentity(session.TenantId, session.UserId));
        var snapshot = await db.Set<Nexora.Modules.Membership.Domain.PortalApplicationDraft>().SingleAsync(d => d.Id == draftId);
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, $"/drafts/{draftId}", HttpMethod.Delete, new { version = snapshot.Version })).StatusCode);
    }

    [Fact]
    public async Task Evidence_upload_is_owner_scoped_and_quarantined_files_cannot_be_downloaded()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        await Grant(app, staff); await PortalLogin(member);
        var product = await Json(await Send(staff, "/api/v1/membership/products", HttpMethod.Post, new { name = "Evidence product", category = "individual", termMonths = 12, rate = 0, currency = "GBP", isActive = true }));
        await Json(await Send(staff, $"/api/v1/membership/products/{Id(product)}/portal", HttpMethod.Put, new { version = Version(product), enabled = true, terms = "Evidence terms" }));
        var draftId = Guid.NewGuid();
        var draft = await Json(await PortalSend(member, $"/drafts/{draftId}", HttpMethod.Put, new { productId = Id(product), startsAtUtc = DateTime.UtcNow.Date, notes = "Evidence notes", version = (string?)null }));
        var csrf = await member.GetFromJsonAsync<CsrfResponse>("/api/v1/portal/csrf");
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(Version(draft)), "version");
        form.Add(new ByteArrayContent("proof"u8.ToArray()), "file", "..\\proof.txt");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/portal/drafts/{draftId}/evidence") { Content = form };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        var upload = await Json(await member.SendAsync(request));
        Assert.Equal("proof.txt", upload["document"]!["name"]!.GetValue<string>());
        Assert.Equal("quarantined", upload["document"]!["scanStatus"]!.GetValue<string>());
        var fileId = upload["document"]!["id"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.BadRequest, (await member.GetAsync($"/api/v1/portal/drafts/{draftId}/evidence/{fileId}")).StatusCode);
        var listed = await Json(await member.GetAsync($"/api/v1/portal/drafts/{draftId}/evidence"));
        Assert.Single(listed.AsArray());
        using var other = app.CreateClient();
        var otherContact = await Contact(staff);
        await Json(await Send(staff, "/api/v1/admin/portal-access/local-member", HttpMethod.Post, new { email = "evidence.other@nexora.test", password = IdentityApiFactory.AdminPassword, contactId = Id(otherContact) }));
        await PortalSend(other, "/login", HttpMethod.Post, new LoginRequest("evidence.other@nexora.test", IdentityApiFactory.AdminPassword, false));
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/portal/drafts/{draftId}/evidence")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await staff.GetAsync($"/api/v1/membership/applications/{Guid.NewGuid()}/evidence")).StatusCode);
    }

    [Fact]
    public async Task Draft_discard_requires_owner_csrf_and_current_version_and_frees_product_slot()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        await Grant(app, staff); await PortalLogin(member);
        var product = await Json(await Send(staff, "/api/v1/membership/products", HttpMethod.Post, new { name = "Discard product", category = "individual", termMonths = 12, rate = 0, currency = "GBP", isActive = true }));
        await Json(await Send(staff, $"/api/v1/membership/products/{Id(product)}/portal", HttpMethod.Put, new { version = Version(product), enabled = true, terms = "Terms" }));
        var id = Guid.NewGuid();
        var payload = new { productId = Id(product), startsAtUtc = DateTime.UtcNow.Date, notes = "Draft notes", version = (string?)null };
        var draft = await Json(await PortalSend(member, $"/drafts/{id}", HttpMethod.Put, payload));
        using var raw = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/portal/drafts/{id}") { Content = JsonContent.Create(new { version = Version(draft) }) };
        Assert.Equal(HttpStatusCode.BadRequest, (await member.SendAsync(raw)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await PortalSend(member, $"/drafts/{id}", HttpMethod.Delete, new { version = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, $"/drafts/{id}", HttpMethod.Delete, new { version = Version(draft), contactId = Guid.NewGuid() })).StatusCode);
        using var other = app.CreateClient(); var otherContact = await Contact(staff);
        await Json(await Send(staff, "/api/v1/admin/portal-access/local-member", HttpMethod.Post, new { email = "discard.other@nexora.test", password = IdentityApiFactory.AdminPassword, contactId = Id(otherContact) }));
        await PortalSend(other, "/login", HttpMethod.Post, new LoginRequest("discard.other@nexora.test", IdentityApiFactory.AdminPassword, false));
        Assert.Equal(HttpStatusCode.NotFound, (await PortalSend(other, $"/drafts/{id}", HttpMethod.Delete, new { version = Version(draft) })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PortalSend(member, $"/drafts/{id}", HttpMethod.Delete, new { version = Version(draft) })).StatusCode);
        Assert.Empty((await Json(await member.GetAsync("/api/v1/portal/drafts"))).AsArray());
        await Json(await PortalSend(member, $"/drafts/{Guid.NewGuid()}", HttpMethod.Put, payload));
        Assert.Single((await Json(await member.GetAsync("/api/v1/portal/drafts"))).AsArray());
        Assert.Empty((await Json(await member.GetAsync("/api/v1/portal/memberships")))["applications"]!.AsArray());
    }

    [Fact]
    public async Task Application_draft_reloads_quotes_rejects_stale_terms_and_submits_exactly_once()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        await Grant(app, staff); Assert.Equal(HttpStatusCode.OK, (await PortalLogin(member)).StatusCode);
        var product = await Json(await Send(staff, "/api/v1/membership/products", HttpMethod.Post, new { name = "Portal application", category = "individual", termMonths = 12, rate = 50, currency = "GBP", isActive = true }));
        Assert.Empty((await Json(await member.GetAsync("/api/v1/portal/application-products"))).AsArray());
        product = await Json(await Send(staff, $"/api/v1/membership/products/{Id(product)}/portal", HttpMethod.Put, new { version = Version(product), enabled = true, terms = "Demo terms version one" }));
        var draftId = Guid.NewGuid();
        var payload = new { productId = Id(product), startsAtUtc = DateTime.UtcNow.Date, notes = "My application", version = (string?)null };
        var draft = await Json(await PortalSend(member, $"/drafts/{draftId}", HttpMethod.Put, payload));
        Assert.Equal(50m, draft["total"]!.GetValue<decimal>());
        using var other = app.CreateClient();
        var otherContact = await Contact(staff);
        await Json(await Send(staff, "/api/v1/admin/portal-access/local-member", HttpMethod.Post, new { email = "other.member@nexora.test", password = IdentityApiFactory.AdminPassword, contactId = Id(otherContact) }));
        Assert.Equal(HttpStatusCode.OK, (await PortalSend(other, "/login", HttpMethod.Post, new LoginRequest("other.member@nexora.test", IdentityApiFactory.AdminPassword, false))).StatusCode);
        Assert.Empty((await Json(await other.GetAsync("/api/v1/portal/drafts"))).AsArray());
        Assert.Equal(HttpStatusCode.NotFound, (await PortalSend(other, $"/drafts/{draftId}/submit", HttpMethod.Post, new { version = Version(draft), termsAccepted = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PortalSend(other, $"/drafts/{draftId}", HttpMethod.Put, new { payload.productId, payload.startsAtUtc, payload.notes, version = Version(draft) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, $"/drafts/{draftId}", HttpMethod.Put, new { payload.productId, payload.startsAtUtc, payload.notes, version = Version(draft), contactId = Id(otherContact) })).StatusCode);
        Assert.Single((await Json(await member.GetAsync("/api/v1/portal/drafts"))).AsArray());
        Assert.Equal(HttpStatusCode.Conflict, (await PortalSend(member, $"/drafts/{draftId}", HttpMethod.Put, payload)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, $"/drafts/{draftId}/submit", HttpMethod.Post, new { version = Version(draft), termsAccepted = false })).StatusCode);
        await Json(await Send(staff, $"/api/v1/membership/products/{Id(product)}/portal", HttpMethod.Put, new { version = Version(product), enabled = true, terms = "Updated terms" }));
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, $"/drafts/{draftId}/submit", HttpMethod.Post, new { version = Version(draft), termsAccepted = true })).StatusCode);
        draft = await Json(await PortalSend(member, $"/drafts/{draftId}", HttpMethod.Put, new { payload.productId, payload.startsAtUtc, payload.notes, version = Version(draft) }));
        var submit = new { version = Version(draft), termsAccepted = true };
        var first = await Json(await PortalSend(member, $"/drafts/{draftId}/submit", HttpMethod.Post, submit));
        var repeated = await Json(await PortalSend(member, $"/drafts/{draftId}/submit", HttpMethod.Post, submit));
        Assert.Equal(first.ToJsonString(), repeated.ToJsonString());
        Assert.Empty((await Json(await member.GetAsync("/api/v1/portal/drafts"))).AsArray());
        var overview = await Json(await member.GetAsync("/api/v1/portal/memberships"));
        Assert.Single(overview["applications"]!.AsArray()); Assert.Empty(overview["memberships"]!.AsArray());
        var application = (await Json(await staff.GetAsync("/api/v1/membership/applications")))["items"]![0]!;
        Assert.Equal("My application", application["applicantNotes"]!.GetValue<string>());
        Assert.Equal("individual", application["applicationType"]!.GetValue<string>());
    }
    private sealed record MemberIdentity(Guid? TenantId, Guid? UserId) : Nexora.BuildingBlocks.Security.IRequestIdentity;
    [Fact]
    public async Task Membership_overview_excludes_other_contacts_tenants_organisations_and_internal_notes()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        var (_, contact) = await Grant(app, staff);
        var session = (await staff.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;
        using var scope = app.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<Nexora.Modules.Membership.Infrastructure.MembershipDbContext>>();
        var contactId = Guid.Parse(Id(contact));
        async Task<Guid> Seed(Guid tenantId, Guid holder, Guid? account, string label)
        {
            using var db = new Nexora.Modules.Membership.Infrastructure.MembershipDbContext(options, new MemberIdentity(tenantId, session.UserId));
            var product = new Nexora.Modules.Membership.Domain.MembershipProduct { TenantId = tenantId, Name = label, Category = "member" };
            var application = new Nexora.Modules.Membership.Domain.MembershipApplication { TenantId = tenantId, ProductId = product.Id, ContactId = holder, AccountId = account, ApplicationType = account == null ? "individual" : "corporate", Status = "approved", DecisionReason = "PRIVATE STAFF NOTE", ApplicantNotes = "PRIVATE APPLICANT NOTE" };
            var subscription = new Nexora.Modules.Membership.Domain.MemberSubscription { TenantId = tenantId, ProductId = product.Id, ApplicationId = application.Id, ContactId = holder, AccountId = account, StartsAtUtc = DateTime.UtcNow.Date, EndsAtUtc = DateTime.UtcNow.Date.AddYears(1) };
            db.AddRange(product, application, subscription); await db.SaveChangesAsync(); return application.Id;
        }
        var ownApplication = await Seed(session.TenantId, contactId, null, "Personal membership");
        var foreignContact = await Seed(session.TenantId, Guid.NewGuid(), null, "Other contact");
        var organisation = await Seed(session.TenantId, contactId, Guid.NewGuid(), "Organisation");
        var foreignTenant = await Seed(Guid.NewGuid(), contactId, null, "Other tenant");
        Assert.Equal(HttpStatusCode.OK, (await PortalLogin(member)).StatusCode);
        var response = await member.GetAsync("/api/v1/portal/memberships");
        var data = await Json(response);
        Assert.Single(data["memberships"]!.AsArray()); Assert.Single(data["applications"]!.AsArray());
        Assert.Equal("Personal membership", data["memberships"]![0]!["product"]!.GetValue<string>());
        Assert.DoesNotContain("PRIVATE", data.ToJsonString()); Assert.DoesNotContain("contactId", data.ToJsonString());
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        foreach (var forbidden in new[] { foreignContact, organisation, foreignTenant })
            Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/v1/portal/applications/{forbidden}")).StatusCode);
        var legacyReceipt = await Json(await member.GetAsync($"/api/v1/portal/applications/{ownApplication}"));
        Assert.Null(legacyReceipt["acceptedTerms"]); Assert.Null(legacyReceipt["submittedNotes"]);
        Assert.DoesNotContain("PRIVATE", legacyReceipt.ToJsonString());
    }
    [Fact]
    public async Task Disabled_local_adapter_rejects_login_and_provisioning()
    {
        using var app = new IdentityApiFactory();
        using var disabled = app.WithWebHostBuilder(b => b.ConfigureTestServices(s => { s.RemoveAll<LocalIdentitySettings>(); s.AddSingleton(new LocalIdentitySettings(false)); }));
        using var client = disabled.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/v1/portal/csrf")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/v1/portal/login", new LoginRequest("nobody@example.test", "unused", false))).StatusCode);
    }

    [Fact]
    public async Task Dedicated_local_member_has_no_staff_permissions_and_contact_binding_is_unique()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        var contact = await Contact(staff);
        var request = new { email = "member@nexora.test", password = IdentityApiFactory.AdminPassword, contactId = Id(contact) };
        await Json(await Send(staff, "/api/v1/admin/portal-access/local-member", HttpMethod.Post, request));
        Assert.Equal(HttpStatusCode.OK, (await PortalSend(member, "/login", HttpMethod.Post, new LoginRequest(request.email, request.password, false))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync("/api/v1/portal/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/v1/admin/access-overview")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, "/api/v1/admin/portal-access/local-member", HttpMethod.Post, new { email = "duplicate@nexora.test", request.password, request.contactId })).StatusCode);
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>(); using var system = db.BeginSystemAccess();
        Assert.False(await db.Users.AnyAsync(x => x.Email == "duplicate@nexora.test"));
        var user = await db.Users.SingleAsync(x => x.Email == request.email);
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Nexora.Modules.Identity.Domain.NexoraUser>>().GetRolesAsync(user));
    }

    [Fact]
    public async Task Staff_and_portal_sessions_coexist_without_csrf_identity_confusion()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app);
        await Grant(app, staff);
        Assert.Equal(HttpStatusCode.OK, (await PortalLogin(staff)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/v1/portal/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PortalSend(staff, "/logout", HttpMethod.Post)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/v1/auth/session")).StatusCode);
    }
    private static async Task<HttpResponseMessage> PortalSend(HttpClient client, string path, HttpMethod method, object? body = null)
    {
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/portal/csrf");
        using var request = new HttpRequestMessage(method, "/api/v1/portal" + path) { Content = body == null ? null : JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token); return await client.SendAsync(request);
    }
    private static async Task<(string Grant, JsonNode Contact)> Grant(IdentityApiFactory app, HttpClient staff)
    {
        var contact = await Contact(staff);
        var session = await staff.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session");
        var grant = await Json(await Send(staff, "/api/v1/admin/portal-access", HttpMethod.Post, new { userId = session!.UserId, contactId = Id(contact) }));
        return (Id(grant), contact);
    }
    private static Task<HttpResponseMessage> PortalLogin(HttpClient client) => PortalSend(client, "/login", HttpMethod.Post, new LoginRequest(IdentityApiFactory.AdminEmail, IdentityApiFactory.AdminPassword, false));

    [Fact]
    public async Task Portal_cookie_is_separate_and_profile_updates_shared_contact_with_allowlist_and_concurrency()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        var (_, contact) = await Grant(app, staff);
        Assert.Equal(HttpStatusCode.Unauthorized, (await staff.GetAsync("/api/v1/portal/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PortalLogin(member)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/v1/crm/records")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/v1/auth/session")).StatusCode);
        var profile = await Json(await member.GetAsync("/api/v1/portal/profile"));
        Assert.Equal(3, profile.AsObject().Count); Assert.Null(profile["tenantId"]); Assert.Null(profile["contactId"]);
        var version = Version(profile);
        Assert.Equal(HttpStatusCode.BadRequest, (await member.PutAsJsonAsync("/api/v1/portal/profile", new { name = "Unsafe", version })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Unsafe", version, contactId = Guid.NewGuid() })).StatusCode);
        await Json(await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Member updated", version }));
        Assert.Equal(HttpStatusCode.Conflict, (await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Stale", version })).StatusCode);
        var shared = await Json(await staff.GetAsync("/api/v1/crm/records/" + Id(contact)));
        Assert.Contains("Member updated", shared.ToJsonString());
        Assert.Equal(HttpStatusCode.NoContent, (await PortalSend(member, "/logout", HttpMethod.Post)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/v1/portal/profile")).StatusCode);
    }

    [Fact]
    public async Task Revocation_takes_effect_on_existing_cookie_and_regrant_does_not_reactivate_it()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        var (grant, contact) = await Grant(app, staff);
        Assert.Equal(HttpStatusCode.OK, (await PortalLogin(member)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(staff, "/api/v1/admin/portal-access/" + grant, HttpMethod.Delete)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/v1/portal/profile")).StatusCode);
        var session = await staff.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session");
        await Json(await Send(staff, "/api/v1/admin/portal-access", HttpMethod.Post, new { userId = session!.UserId, contactId = Id(contact) }));
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/v1/portal/profile")).StatusCode);
    }

    [Fact]
    public async Task Grants_reject_unknown_cross_tenant_users_and_duplicate_bindings()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var outside = await Login(app, "outside@nexora.test"); using var anonymous = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await PortalLogin(anonymous)).StatusCode);
        var (_, contact) = await Grant(app, staff);
        var foreign = await outside.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session");
        Assert.Equal(HttpStatusCode.NotFound, (await Send(staff, "/api/v1/admin/portal-access", HttpMethod.Post, new { userId = foreign!.UserId, contactId = Id(contact) })).StatusCode);
        var session = await staff.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session");
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, "/api/v1/admin/portal-access", HttpMethod.Post, new { userId = session!.UserId, contactId = Id(contact) })).StatusCode);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("tenant")]
    [InlineData("stamp")]
    public async Task Live_identity_changes_reject_existing_cookie(string change)
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        await Grant(app, staff); Assert.Equal(HttpStatusCode.OK, (await PortalLogin(member)).StatusCode);
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>();
        using var system = db.BeginSystemAccess(); var user = await db.Users.SingleAsync(x => x.Email == IdentityApiFactory.AdminEmail);
        if (change == "user") user.IsActive = false;
        if (change == "stamp") user.SecurityStamp = Guid.NewGuid().ToString();
        if (change == "tenant") (await db.Tenants.SingleAsync(x => x.Id == user.TenantId)).Status = Nexora.Modules.Identity.Domain.TenantStatus.Suspended;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/v1/portal/profile")).StatusCode);
    }
}

