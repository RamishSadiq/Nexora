using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Nexora.Modules.Identity;
namespace Nexora.Api.Tests;
internal static class WorkflowTestClient
{
    public static async Task<HttpClient> Login(IdentityApiFactory app, string email = IdentityApiFactory.AdminEmail)
    {
        var client = app.CreateClient();
        await Json(await Send(client,"/api/v1/auth/login",HttpMethod.Post,new LoginRequest(email,IdentityApiFactory.AdminPassword,false)));
        return client;
    }
    public static async Task<HttpResponseMessage> Send(HttpClient client,string path,HttpMethod method,object? body=null)
    {
        var token=await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var request=new HttpRequestMessage(method,path){Content=body==null?null:JsonContent.Create(body)};
        request.Headers.Add("X-CSRF-TOKEN",token!.Token);return await client.SendAsync(request);
    }
    public static async Task<JsonNode> Json(HttpResponseMessage response,HttpStatusCode expected=HttpStatusCode.OK)
    {
        var body=await response.Content.ReadAsStringAsync();Assert.True(response.StatusCode==expected,$"Expected {expected}, got {response.StatusCode}: {body}");return JsonNode.Parse(body)!;
    }
    public static string Id(JsonNode row) => row["id"]!.GetValue<string>();
    public static string Version(JsonNode row) => row["version"]!.GetValue<string>();
    public static async Task<JsonNode> Contact(HttpClient client) => await Json(await Send(client,"/api/v1/crm/records",HttpMethod.Post,new{kind="contact",name="Workflow Contact",status="active"}),HttpStatusCode.Created);
}
