using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Workflow;
public sealed class WorkflowConnections(WorkflowDbContext db,IDataProtectionProvider protection)
{
 private IDataProtector Protector=>protection.CreateProtector("Nexora.Workflow.Connectors.v1");
 public string Protect(string secret)=>Protector.Protect(secret);
 public string Unprotect(string secret)=>string.IsNullOrEmpty(secret)?"":Protector.Unprotect(secret);
 public static bool Public(IPAddress address)
 {
  if(address.IsIPv4MappedToIPv6)address=address.MapToIPv4();if(IPAddress.IsLoopback(address)||address.Equals(IPAddress.Any)||address.Equals(IPAddress.IPv6Any)||address.IsIPv6LinkLocal||address.IsIPv6SiteLocal||address.IsIPv6Multicast)return false;
  var bytes=address.GetAddressBytes();if(bytes.Length==16)return bytes[0]!=0xfc&&bytes[0]!=0xfd&&bytes.Take(12).Any(x=>x!=0)&&bytes[0] is >=0x20 and <=0x3f;return bytes[0] is not (0 or 10 or 127)&&!(bytes[0]==169&&bytes[1]==254)&&!(bytes[0]==172&&bytes[1] is >=16 and <=31)&&!(bytes[0]==192&&bytes[1]==168)&&bytes[0]<224&&!(bytes[0]==100&&bytes[1] is >=64 and <=127);
 }
 public static Uri Endpoint(string endpoint){if(!Uri.TryCreate(endpoint,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.UserInfo!=""||uri.Fragment!=""||uri.HostNameType==UriHostNameType.Unknown)throw new ValidationException("Use an approved HTTPS endpoint without embedded credentials.");if(IPAddress.TryParse(uri.Host,out var address)&&!Public(address))throw new ValidationException("Private and local endpoints are not allowed.");return uri;}
 public static HttpClient Client()
 {
  var handler=new SocketsHttpHandler{AllowAutoRedirect=false,ConnectTimeout=TimeSpan.FromSeconds(10),ConnectCallback=async(context,ct)=>{var addresses=await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host,ct);if(addresses.Length==0||addresses.Any(x=>!Public(x)))throw new ValidationException("The endpoint resolves to a private or unsupported address.");var socket=new Socket(SocketType.Stream,ProtocolType.Tcp);try{await socket.ConnectAsync(addresses[0],context.DnsEndPoint.Port,ct);return new NetworkStream(socket,true);}catch{socket.Dispose();throw;}}};return new HttpClient(handler){Timeout=TimeSpan.FromSeconds(30)};
 }
 public async Task<WorkflowConnector> FindAsync(Guid id,string kind,CancellationToken ct)=>await db.Set<WorkflowConnector>().SingleOrDefaultAsync(x=>x.Id==id&&x.Kind==kind&&x.IsActive,ct)??throw new ValidationException("The approved connector is unavailable.");
 public async Task<string> GenerateAsync(Guid connectorId,string prompt,object metadata,CancellationToken ct)
 {
  if(string.IsNullOrWhiteSpace(prompt)||prompt.Length>4000)throw new ValidationException("Describe the workflow in at most 4,000 characters.");var connector=await FindAsync(connectorId,"ai",ct);using var client=Client();client.DefaultRequestHeaders.Authorization=new("Bearer",Unprotect(connector.ProtectedSecret));using var response=await client.PostAsJsonAsync(Endpoint(connector.Endpoint),new{model=connector.Model,response_format=new{type="json_object"},messages=new[]{new{role="system",content="Return JSON {name,description,graph}. Only a draft. Graph schemaVersion:2, entityKey,trigger,entryNodeId,nodes. Nodes id,kind,label,next,ifTrue,ifFalse,predicate,actionKey,inputs,delayMinutes. Use ONLY the provided entity fields and actions; do not invent identifiers or credentials. Conditions use {field,operator,value,all,any}. Metadata: "+JsonSerializer.Serialize(metadata)},new{role="user",content=prompt}},max_tokens=8000},ct);if(!response.IsSuccessStatusCode)throw new ValidationException("AI provider rejected the request. Check its connector configuration.");var json=await LimitedBody(response,ct);var content=JsonNode.Parse(json)?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();return content??throw new ValidationException("AI provider returned no workflow draft.");
 }
 public static async Task<string> LimitedBody(HttpResponseMessage response,CancellationToken ct)
 {
  if(response.Content.Headers.ContentLength>262144)throw new ValidationException("Provider response is too large.");await using var stream=await response.Content.ReadAsStreamAsync(ct);using var buffer=new MemoryStream();var bytes=new byte[8192];int count;while((count=await stream.ReadAsync(bytes,ct))>0){if(buffer.Length+count>262144)throw new ValidationException("Provider response is too large.");buffer.Write(bytes,0,count);}return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
 }
}
public sealed class WorkflowDeliveryAction(WorkflowDbContext db,WorkflowConnections connections,IRequestIdentity who,string kind,IWorkflowTransport transport):IWorkflowActionHandler
{
 public WorkflowActionDescriptor Descriptor=>kind=="http"?new("integration.http.post","Call approved external API","workflows.integrations",["connectorId","path","body"],[new("connectorId","Connector","reference",true),new("path","Relative path"),new("body","JSON body","json",true)],["statusCode","body","bodyTruncated"]):new("communication.email.send","Send template email","workflows.integrations",["connectorId","templateId","recipient","subject","body"],[new("connectorId","SMTP connector","reference",true),new("templateId","Template","reference"),new("recipient","Recipient email","text",true),new("subject","Subject override"),new("body","Body override")],["deliveryId","status"]);
 public Task ValidateAsync(Dictionary<string,string> inputs,CancellationToken ct){if(!Guid.TryParse(inputs.GetValueOrDefault("connectorId"),out _)||inputs.Keys.Any(x=>!Descriptor.Inputs.Contains(x)))throw new ValidationException("Choose an approved connector and supported inputs.");if(kind=="http"){var path=inputs.GetValueOrDefault("path","");if(path.Contains("..")||path.StartsWith('/')||path.Contains(':')||path.Contains('\\'))throw new ValidationException("Use a relative path beneath the approved endpoint.");try{JsonNode.Parse(inputs.GetValueOrDefault("body","{}"));}catch{throw new ValidationException("HTTP body must be valid JSON.");}}else{try{var recipient=new MailAddress(inputs.GetValueOrDefault("recipient", ""));if(recipient.Address!=inputs["recipient"])throw new Exception();}catch{throw new ValidationException("Enter one valid recipient email.");}}return Task.CompletedTask;}
 public async Task<WorkflowActionResult> ExecuteDetailedAsync(WorkflowActionContext context,Dictionary<string,string> inputs,CancellationToken ct)
 {
  var prior=await db.Set<WorkflowDelivery>().SingleOrDefaultAsync(x=>x.Id==context.ActionId,ct);if(prior?.Status=="delivered")return JsonSerializer.Deserialize<WorkflowActionResult>(prior.ResultJson)!;
  if(prior!=null&&prior.Status!="ready")throw new ValidationException("Delivery needs reconciliation before replay. Its provider may already have received it.");await ValidateAsync(inputs,ct);var connector=await connections.FindAsync(Guid.Parse(inputs["connectorId"]),kind=="http"?"http":"smtp",ct);
  var request=new Dictionary<string,string>(inputs);
  if(kind=="email"&&Guid.TryParse(inputs.GetValueOrDefault("templateId"),out var templateId)){var template=await db.Set<WorkflowEmailTemplate>().SingleOrDefaultAsync(x=>x.Id==templateId,ct)??throw new ValidationException("Email template is unavailable.");request.TryAdd("subject",template.Subject);request.TryAdd("body",template.Body);}
  if(kind=="email"&&(string.IsNullOrWhiteSpace(request.GetValueOrDefault("subject"))||string.IsNullOrWhiteSpace(request.GetValueOrDefault("body"))))throw new ValidationException("Email requires a subject and body or a template.");
  var delivery=prior??new WorkflowDelivery{Id=context.ActionId,TenantId=who.TenantId!.Value,RunId=context.RunId,ActorId=context.ActorId,ConnectorId=connector.Id,Kind=kind,Status="sending",StartedAtUtc=DateTime.UtcNow,RequestJson=JsonSerializer.Serialize(request)};if(prior==null)db.Add(delivery);else{delivery.Status="sending";delivery.StartedAtUtc=DateTime.UtcNow;}await db.SaveChangesAsync(ct);
  try
  {
   var result=await transport.DeliverAsync(connector,request,context.ActionId,kind,ct);
   delivery.Status="delivered";delivery.ResultJson=JsonSerializer.Serialize(result);delivery.FinishedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync(ct);return result;
  }
  catch(Exception ex){delivery.Status="unknown";delivery.Error=ex is ValidationException?ex.Message:"Provider outcome is unknown. Reconcile before retrying.";await db.SaveChangesAsync(CancellationToken.None);throw new ValidationException(delivery.Error);}
 }
 public async Task<string> ExecuteAsync(WorkflowActionContext context,Dictionary<string,string> inputs,CancellationToken ct)=>(await ExecuteDetailedAsync(context,inputs,ct)).Message;
}

public interface IWorkflowTransport { Task<WorkflowActionResult> DeliverAsync(WorkflowConnector connector,Dictionary<string,string> request,Guid actionId,string kind,CancellationToken ct); }
public sealed class WorkflowTransport(WorkflowConnections connections):IWorkflowTransport
{
 public async Task<WorkflowActionResult> DeliverAsync(WorkflowConnector connector,Dictionary<string,string> request,Guid actionId,string kind,CancellationToken ct)
 {
   WorkflowActionResult result;
   if(kind=="http")
   {
    var baseUri=WorkflowConnections.Endpoint(connector.Endpoint);var uri=new Uri(baseUri.AbsoluteUri.EndsWith('/')?baseUri:new Uri(baseUri.AbsoluteUri+"/"),request.GetValueOrDefault("path",""));if(uri.Host!=baseUri.Host||uri.Port!=baseUri.Port)throw new ValidationException("External path exceeds its approved connector.");using var client=WorkflowConnections.Client();using var message=new HttpRequestMessage(HttpMethod.Post,uri){Content=new StringContent(request["body"],System.Text.Encoding.UTF8,"application/json")};message.Headers.Add("Idempotency-Key",actionId.ToString());if(connector.ProtectedSecret!="")message.Headers.Authorization=new("Bearer",connections.Unprotect(connector.ProtectedSecret));using var response=await client.SendAsync(message,HttpCompletionOption.ResponseHeadersRead,ct);var body=await WorkflowConnections.LimitedBody(response,ct);var secret=connections.Unprotect(connector.ProtectedSecret);if(secret!="")body=body.Replace(secret,"[redacted]",StringComparison.Ordinal);if(!response.IsSuccessStatusCode)throw new ValidationException("External provider rejected the action ("+(int)response.StatusCode+"). Reconcile the delivery before retrying.");result=new("External call delivered",new(){{"statusCode",((int)response.StatusCode).ToString()},{"body",body.Length>16000?body[..16000]:body},{"bodyTruncated",body.Length>16000?"true":"false"}});
   }
   else
   {
    var addresses=await Dns.GetHostAddressesAsync(connector.Endpoint,ct);if(addresses.Length==0||addresses.Any(x=>!WorkflowConnections.Public(x)))throw new ValidationException("SMTP host resolves to a private or unsupported address.");using var smtp=new SmtpClient(connector.Endpoint,connector.Port){EnableSsl=true,Credentials=new NetworkCredential(connector.Username,connections.Unprotect(connector.ProtectedSecret)),Timeout=30000};using var mail=new MailMessage(connector.Sender,request["recipient"],request["subject"],request["body"]);mail.Headers.Add("Message-ID","<"+actionId+"@nexora.workflow>");using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(30));await smtp.SendMailAsync(mail,timeout.Token);result=new("Email accepted by SMTP server",new(){{"deliveryId",actionId.ToString()},{"status","delivered"}});
   }
   return result;
 }
}
