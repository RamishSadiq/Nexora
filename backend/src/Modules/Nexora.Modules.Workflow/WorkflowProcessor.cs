using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Workflow;
public sealed class WorkflowProcessor(WorkflowDbContext db,IEnumerable<IWorkflowActionHandler> handlers,WorkflowValidation validation,WorkflowExpressions expressions,WorkflowGates gates)
{
 public static Guid ActionId(Guid runId,string nodeId)=>new(SHA256.HashData(Encoding.UTF8.GetBytes(runId+":"+nodeId))[..16]);
 public async Task StepAsync(Guid id,string[] permissions,CancellationToken ct)
 {
  var run=await db.Set<WorkflowRun>().SingleAsync(x=>x.Id==id,ct);if(run.Status is "cancelled" or "completed" or "failed")return;
  if(run.ParentRunId!=null&&await db.Set<WorkflowRun>().AnyAsync(x=>x.Id==run.ParentRunId&&x.Status=="cancelled",ct)){run.Status="cancelled";run.FinishedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync(ct);return;}
  WorkflowToken? token=null;WorkflowNode? node=null;
  try
  {
   if(!permissions.Contains("workflows.read")||!permissions.Contains("workflows.execute"))throw new ValidationException("Execution permission was revoked.");
   if(run.Steps>=10000)throw new ValidationException("Execution limit reached.");
   var version=await db.Set<WorkflowVersion>().SingleAsync(x=>x.Id==run.WorkflowVersionId,ct);var graph=WorkflowValidation.Parse(version.GraphJson);
   var errors=await validation.ValidateAsync(graph,permissions,ct,false);if(errors.Length>0)throw new ValidationException(string.Join(" ",errors));
   var tokens=await db.Set<WorkflowToken>().Where(x=>x.RunId==id).OrderBy(x=>x.CreatedAtUtc).ToListAsync(ct);
   if(tokens.Count==0){token=new(){Id=run.Id,TenantId=run.TenantId,RunId=id,NodeId=run.CurrentNodeId,ContextJson=run.ContextJson};db.Add(token);tokens.Add(token);}
   // Resolve durable joins and subworkflow completions before choosing the next runnable branch.
   foreach(var parent in tokens.Where(x=>x.Status=="blocked"))
   {
    var context=WorkflowExpressions.Object(parent.ContextJson);
    if(context["childRunId"]!=null){var childId=Guid.Parse(WorkflowExpressions.Text(context["childRunId"]));var child=await db.Set<WorkflowRun>().SingleAsync(x=>x.Id==childId,ct);if(child.Status is "failed" or "cancelled")throw new ValidationException("Subworkflow did not complete: "+child.Error);if(child.Status!="completed")continue;context.Remove("childRunId");context["steps"]![parent.PendingNodeId]=new JsonObject{["runId"]=child.Id.ToString(),["status"]=child.Status};parent.NodeId=graph.Nodes.Single(x=>x.Id==parent.PendingNodeId).Next!;}
    else{var children=tokens.Where(x=>x.ParentTokenId==parent.Id).ToArray();if(children.Any(x=>x.Status=="failed"))throw new ValidationException("A parallel branch failed.");if(children.Length==0||children.Any(x=>x.Status!="completed"))continue;var branches=new JsonObject();foreach(var child in children)branches[child.Scope]=WorkflowExpressions.Object(child.ContextJson)["variables"]?.DeepClone();context["branches"]=branches;}
    parent.Status="queued";parent.ContextJson=WorkflowExpressions.BoundedJson(context);
   }
   token=tokens.FirstOrDefault(x=>(x.Status=="queued"||x.Status=="waiting")&&(x.DueAtUtc==null||x.DueAtUtc<=DateTime.UtcNow));
   if(token==null){if(tokens.All(x=>x.Status=="completed")){run.Status="completed";run.FinishedAtUtc=DateTime.UtcNow;}else{run.Status="waiting";run.DueAtUtc=tokens.Where(x=>x.DueAtUtc!=null).Select(x=>x.DueAtUtc).Min()??DateTime.UtcNow.AddSeconds(2);}run.LeaseUntilUtc=null;await db.SaveChangesAsync(ct);return;}
   node=graph.Nodes.Single(x=>x.Id==token.NodeId);var traceInputs=new JsonObject();var traceOutputs=new JsonObject();var ctx=WorkflowExpressions.Object(token.ContextJson);var frames=JsonNode.Parse(token.FramesJson) as JsonArray??new();
   if(!ctx.ContainsKey("record")){var trigger=WorkflowExpressions.Object(run.TriggerJson);ctx["record"]=trigger["record"]?.DeepClone()??await expressions.ReadRecordAsync(graph.EntityKey,run.RecordId,permissions,ct);ctx["previous"]=trigger["previous"]?.DeepClone();ctx["trigger"]=trigger;ctx["actor"]=new JsonObject{["id"]=run.ActorId.ToString()};ctx["owner"]=new JsonObject{["id"]=ctx["record"]?["ownerUserId"]?.DeepClone()};ctx["variables"]=new JsonObject();foreach(var pair in graph.Variables??new())ctx["variables"]![pair.Key]=WorkflowExpressions.Literal(pair.Value);foreach(var pair in WorkflowExpressions.Object(run.ContextJson)){if(pair.Key=="variables"&&pair.Value is JsonObject supplied){foreach(var variable in supplied)ctx["variables"]![variable.Key]=variable.Value?.DeepClone();}else ctx[pair.Key]=pair.Value?.DeepClone();}ctx["steps"]=new JsonObject();}
   else if(node.Kind is not ("end" or "return" or "join")&&WorkflowExpressions.Text(ctx["record"]?["isArchived"])!="true"&&WorkflowExpressions.Text(ctx["trigger"]?["kind"])!="record.deleted")ctx["record"]=await expressions.ReadRecordAsync(graph.EntityKey,run.RecordId,permissions,ct);
   ctx["now"]=DateTime.UtcNow.ToString("O");string outcome="";
   void Advance(string? next){token.NodeId=next??throw new ValidationException("Next node is missing.");token.Status="queued";token.DueAtUtc=null;}
   void Return()
   {
    if(frames.Count==0){token.Status="completed";return;}var frame=(JsonObject)frames[^1]!;var items=(JsonArray)frame["items"]!;var index=frame["index"]!.GetValue<int>()+1;
    if(index<items.Count){frame["index"]=index;ctx["item"]=items[index]?.DeepClone();token.Scope=WorkflowExpressions.Text(frame["scope"])+"/"+WorkflowExpressions.Text(frame["node"])+"/"+index;Advance(WorkflowExpressions.Text(frame["body"]));}
    else{ctx["item"]=frame["previousItem"]?.DeepClone();token.Scope=WorkflowExpressions.Text(frame["scope"]);var next=WorkflowExpressions.Text(frame["next"]);frames.RemoveAt(frames.Count-1);Advance(next);}
   }
   switch(node.Kind)
   {
    case "approval":case "await-condition":
     var gate=await gates.ProcessAsync(run,token,node,graph,ctx,permissions,ct);outcome=gate.Outcome;
     if(!gate.Pending){Advance(gate.Destination);traceOutputs=ctx["steps"]![node.Id] as JsonObject??new();}
     else if(!gate.NewRequest){token.ContextJson=WorkflowExpressions.BoundedJson(ctx);run.LeaseUntilUtc=null;var otherReady=tokens.Any(t=>t.Id!=token.Id&&t.Status=="queued");run.Status=otherReady?"queued":"waiting";run.DueAtUtc=otherReady?null:tokens.Where(t=>t.Status=="waiting").Select(t=>t.DueAtUtc).Min();await db.SaveChangesAsync(ct);return;}
     break;
    case "end":case "return":Return();outcome=token.Status=="completed"?"Completed":"Loop continued";break;
    case "condition":var matched=await expressions.EvaluateAsync(node.Predicate??new(node.Field,node.Operator,node.Value),ctx,graph.EntityKey,permissions,ct);Advance(matched?node.IfTrue:node.IfFalse);outcome=matched?"True":"False";break;
    case "wait":DateTime due;if(node.WaitUntil!=null){if(!DateTime.TryParse(WorkflowExpressions.Text(await expressions.ResolveAsync(node.WaitUntil,ctx,graph.EntityKey,permissions,ct)),null,System.Globalization.DateTimeStyles.AdjustToUniversal|System.Globalization.DateTimeStyles.AssumeUniversal,out due))throw new ValidationException("Wait date is invalid.");}else due=DateTime.UtcNow.AddMinutes(node.DelayMinutes!.Value);Advance(node.Next);token.Status="waiting";token.DueAtUtc=due;outcome="Resume at "+due.ToString("O");break;
    case "variable":ctx["variables"]![node.OutputVariable!]=await expressions.ResolveAsync(node.Value,ctx,graph.EntityKey,permissions,ct);Advance(node.Next);outcome="Variable set: "+node.OutputVariable;break;
    case "query":var source=expressions.Source(node.QueryEntity!,permissions);var predicate=node.Predicate==null?null:await expressions.ResolvePredicateAsync(node.Predicate,ctx,graph.EntityKey,permissions,ct);var page=await source.QueryAsync(predicate,node.Limit,ct);if(page.Total>node.Limit)throw new ValidationException("Query exceeds its limit. Narrow its filters.");ctx["variables"]![node.OutputVariable!]=new JsonArray(page.Items.Select(r=>(JsonNode)WorkflowExpressions.ToJson(r,source.Metadata)).ToArray());Advance(node.Next);outcome="Queried "+page.Total+" records";break;
    case "loop":var list=await expressions.ResolveAsync("{{"+node.ItemsVariable+"}}",ctx,graph.EntityKey,permissions,ct) as JsonArray??throw new ValidationException("Loop requires a list.");if(list.Count>1000||frames.Count>=8)throw new ValidationException("Loop limit exceeded.");if(list.Count==0)Advance(node.Next);else{frames.Add(new JsonObject{["node"]=node.Id,["body"]=node.Body,["next"]=node.Next,["scope"]=token.Scope,["items"]=list.DeepClone(),["index"]=0,["previousItem"]=ctx["item"]?.DeepClone()});ctx["item"]=list[0]?.DeepClone();token.Scope+="/"+node.Id+"/0";Advance(node.Body);}outcome="Loop: "+list.Count+" items";break;
    case "parallel":foreach(var branch in node.Branches!){var scope=token.Scope+"/"+node.Id+"/"+branch;db.Add(new WorkflowToken{Id=ActionId(run.Id,scope),TenantId=run.TenantId,RunId=id,ParentTokenId=token.Id,NodeId=branch,Scope=scope,ContextJson=WorkflowExpressions.BoundedJson(ctx)});}token.NodeId=node.Next!;token.Status="blocked";outcome="Parallel branches started";break;
    case "join":if(token.ParentTokenId!=null)token.Status="completed";else Advance(node.Next);outcome="Joined";break;
    case "subworkflow":var childVersion=await db.Set<WorkflowVersion>().SingleAsync(x=>x.Id==Guid.Parse(node.WorkflowVersionId!),ct);var ancestry=JsonSerializer.Deserialize<Guid[]>(run.AncestryJson)??[];if(ancestry.Contains(childVersion.DefinitionId)||ancestry.Length>=8)throw new ValidationException("Recursive subworkflow is prohibited.");var childGraph=WorkflowValidation.Parse(childVersion.GraphJson);var childRecord=node.Value==null?run.RecordId:Guid.Parse(WorkflowExpressions.Text(await expressions.ResolveAsync(node.Value,ctx,graph.EntityKey,permissions,ct)));var childObject=await expressions.ReadRecordAsync(childGraph.EntityKey,childRecord,permissions,ct);var childRun=new WorkflowRun{Id=ActionId(run.Id,token.Scope+":"+node.Id),TenantId=run.TenantId,WorkflowVersionId=childVersion.Id,ActorId=run.ActorId,RecordId=childRecord,RecordLabel=WorkflowExpressions.Text(childObject["label"]),ParentRunId=id,RootRunId=run.RootRunId??id,AncestryJson=JsonSerializer.Serialize(ancestry.Append(childVersion.DefinitionId)),RequestKey="sub:"+token.Id+":"+node.Id,CurrentNodeId=childGraph.EntryNodeId};db.Add(childRun);ctx["childRunId"]=childRun.Id.ToString();token.PendingNodeId=node.Id;token.Status="blocked";outcome="Subworkflow started: "+childRun.Id;break;
    case "action":
     var handler=handlers.Single(x=>x.Descriptor.Key==node.ActionKey);if(!permissions.Contains(handler.Descriptor.Permission))throw new ValidationException("Action permission was revoked.");
     Dictionary<string,string> inputs;
     if(token.PendingNodeId==node.Id&&token.PendingInputsJson!="")inputs=JsonSerializer.Deserialize<Dictionary<string,string>>(token.PendingInputsJson)!;
     else{inputs=new();var rawInputs=new Dictionary<string,string>(node.Inputs??new());if(node.ActionKey=="communication.email.send"&&Guid.TryParse(rawInputs.GetValueOrDefault("templateId"),out var templateId)){var template=await db.Set<WorkflowEmailTemplate>().SingleOrDefaultAsync(x=>x.Id==templateId,ct)??throw new ValidationException("Email template is unavailable.");rawInputs.TryAdd("subject",template.Subject);rawInputs.TryAdd("body",template.Body);}foreach(var pair in rawInputs)inputs[pair.Key]=WorkflowExpressions.Text(await expressions.ResolveAsync(pair.Value,ctx,graph.EntityKey,permissions,ct));token.PendingNodeId=node.Id;token.PendingInputsJson=JsonSerializer.Serialize(inputs);token.ContextJson=WorkflowExpressions.BoundedJson(ctx);await db.SaveChangesAsync(ct);}
     foreach(var pair in inputs)traceInputs[pair.Key]=pair.Value;var result=await handler.ExecuteDetailedAsync(new(run.Id,node.Id,ActionId(run.Id,token.Scope==""?node.Id:token.Scope+":"+node.Id),run.ActorId,run.RecordLabel,run.RecordId),inputs,ct);var outputs=new JsonObject();foreach(var pair in result.Outputs){try{outputs[pair.Key]=WorkflowExpressions.Literal(pair.Value);}catch(ValidationException){outputs[pair.Key]=JsonValue.Create(pair.Value);}}ctx["steps"]![node.Id]=outputs;traceOutputs=(JsonObject)outputs.DeepClone();if(node.ActionKey=="crm.record.archive"&&inputs.GetValueOrDefault("recordId")==run.RecordId.ToString())ctx["record"]!["isArchived"]="true";outcome=result.Message;token.PendingInputsJson="";token.PendingNodeId="";Advance(node.Next);break;
    default:throw new ValidationException("Unknown node kind.");
   }
   token.ContextJson=WorkflowExpressions.BoundedJson(ctx);token.FramesJson=WorkflowExpressions.BoundedJson(frames);token.Steps++;token.Attempts=0;token.Error="";token.LeaseUntilUtc=null;run.ContextJson=token.ContextJson;run.CurrentNodeId=token.NodeId;run.LeaseUntilUtc=null;run.Attempts=0;run.Error="";run.Status=token.Status=="waiting"&&tokens.Where(x=>x.Id!=token.Id).All(x=>x.Status=="completed")?"waiting":token.Status=="completed"&&tokens.Where(x=>x.Id!=token.Id).All(x=>x.Status=="completed")?"completed":"queued";run.DueAtUtc=run.Status=="waiting"?token.DueAtUtc:null;if(run.Status=="completed"){run.FinishedAtUtc=DateTime.UtcNow;if(run.ParentRunId!=null)await db.Set<WorkflowRun>().Where(x=>x.Id==run.ParentRunId&&x.Status=="waiting").ExecuteUpdateAsync(s=>s.SetProperty(x=>x.DueAtUtc,(DateTime?)null).SetProperty(x=>x.Status,"queued"),ct);}
   db.Add(new WorkflowStep{TenantId=run.TenantId,RunId=id,NodeId=node.Id,Kind=node.Kind,Scope=token.Scope,InputsJson=traceInputs.ToJsonString(),OutputsJson=traceOutputs.ToJsonString(),Outcome=outcome.Length>500?outcome[..500]:outcome,Sequence=++run.Steps});await db.SaveChangesAsync(ct);
  }
  catch(DbUpdateConcurrencyException){db.ChangeTracker.Clear();}
  catch(OperationCanceledException) when(ct.IsCancellationRequested){throw;}
  catch(Exception ex)
  {
   var tokenId=token?.Id;db.ChangeTracker.Clear();run=await db.Set<WorkflowRun>().SingleAsync(x=>x.Id==id,ct);if(run.Status is "cancelled" or "completed")return;token=tokenId==null?null:await db.Set<WorkflowToken>().SingleOrDefaultAsync(x=>x.Id==tokenId,ct);
   var message=ex is ValidationException?ex.Message:"The step failed. Check service health before retrying.";run.Error=message.Length>500?message[..500]:message;run.Attempts++;run.LeaseUntilUtc=null;
   if(token!=null&&node?.OnError is "skip" or "continue" or "escalate"){token.NodeId=node.ErrorNext??node.Next??throw new ValidationException("Error handling requires a destination.");token.Status="queued";token.PendingInputsJson="";token.Error=run.Error;run.Status="queued";run.DueAtUtc=null;}
   else if(token!=null&&node?.OnError=="retry"&&++token.Attempts<5){token.Status="waiting";token.DueAtUtc=DateTime.UtcNow.AddSeconds(Math.Pow(2,token.Attempts)*5);run.Status="queued";run.DueAtUtc=token.DueAtUtc;}
   else{if(token!=null){token.Status="failed";token.Error=run.Error;}run.Status="failed";run.FinishedAtUtc=DateTime.UtcNow;}
   db.Add(new WorkflowStep{TenantId=run.TenantId,RunId=id,NodeId=node?.Id??run.CurrentNodeId,Kind="failure",Outcome=run.Error,Sequence=++run.Steps});await db.SaveChangesAsync(ct);
  }
 }
}
public sealed class WorkflowWorker(IServiceScopeFactory scopes,WorkflowExecutionContext execution,ILogger<WorkflowWorker> logger):BackgroundService
{
 protected override async Task ExecuteAsync(CancellationToken stoppingToken){while(!stoppingToken.IsCancellationRequested){try{await TickAsync(stoppingToken);}catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}catch(Exception e){logger.LogError(e,"Workflow processing failed; pending work remains durable");}await Task.Delay(TimeSpan.FromSeconds(2),stoppingToken);}}
 public async Task TickAsync(CancellationToken ct)
 {
  await using var discovery=scopes.CreateAsyncScope();await discovery.ServiceProvider.GetRequiredService<WorkflowTriggers>().TickAsync(ct);var db=discovery.ServiceProvider.GetRequiredService<WorkflowDbContext>();var now=DateTime.UtcNow;
  var candidates=await db.Set<WorkflowRun>().IgnoreQueryFilters().AsNoTracking().Where(x=>(x.Status=="queued"||x.Status=="waiting")&&(x.DueAtUtc==null||x.DueAtUtc<=now)&&(x.LeaseUntilUtc==null||x.LeaseUntilUtc<now)).OrderBy(x=>x.CreatedAtUtc).Take(20).Select(x=>new{x.Id,x.TenantId,x.ActorId,x.RootRunId,x.AncestryJson}).ToListAsync(ct);
  foreach(var candidate in candidates)
  {
   var claimed=await db.Set<WorkflowRun>().IgnoreQueryFilters().Where(x=>x.Id==candidate.Id&&x.TenantId==candidate.TenantId&&(x.Status=="queued"||x.Status=="waiting")&&(x.DueAtUtc==null||x.DueAtUtc<=now)&&(x.LeaseUntilUtc==null||x.LeaseUntilUtc<now)).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.LeaseUntilUtc,now.AddMinutes(3)),ct);if(claimed!=1)continue;
   await using var scope=scopes.CreateAsyncScope();var principal=await scope.ServiceProvider.GetRequiredService<IWorkflowPrincipalDirectory>().ResolveAsync(candidate.TenantId,candidate.ActorId,ct);
   using var identity=execution.Enter((principal??new(candidate.TenantId,candidate.ActorId,[])) with{RunId=candidate.Id,RootRunId=candidate.RootRunId??candidate.Id,Ancestry=JsonSerializer.Deserialize<Guid[]>(candidate.AncestryJson)});
   await scope.ServiceProvider.GetRequiredService<WorkflowProcessor>().StepAsync(candidate.Id,principal?.Permissions??[],ct);
  }
 }
}
