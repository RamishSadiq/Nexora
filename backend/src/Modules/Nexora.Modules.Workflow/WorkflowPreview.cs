using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Workflow;
public sealed class WorkflowPreview(WorkflowExpressions expressions,WorkflowValidation validation,IEnumerable<IWorkflowActionHandler> handlers)
{
 public async Task<object> TestAsync(WorkflowTest request,string[] permissions,CancellationToken ct)
 {
  var errors=await validation.ValidateAsync(request.Graph,permissions,ct);if(errors.Length>0)throw new ValidationException(string.Join(" ",errors));var graph=request.Graph;var record=await expressions.ReadRecordAsync(graph.EntityKey,request.RecordId,permissions,ct);var variables=new JsonObject();foreach(var pair in graph.Variables??new())variables[pair.Key]=WorkflowExpressions.Literal(pair.Value);foreach(var pair in request.Variables??new())variables[pair.Key]=WorkflowExpressions.Literal(pair.Value);var context=new JsonObject{["record"]=record,["variables"]=variables,["steps"]=new JsonObject(),["now"]=DateTime.UtcNow.ToString("O"),["trigger"]=new JsonObject{["kind"]="test"}};var trace=new List<object>();var visited=0;
  async Task Walk(string id,JsonObject ctx,int depth,bool branch=false)
  {
   if(depth>8)throw new ValidationException("Preview nesting exceeds 8 levels.");while(true){if(++visited>1000)throw new ValidationException("Preview exceeds 1,000 steps; narrow the query or loop.");var node=graph.Nodes.Single(n=>n.Id==id);object detail="";string? next=node.Next;
    switch(node.Kind)
    {
     case "approval":trace.Add(new{nodeId=id,node.Kind,outcome="Explicit human decision required; branches are not executed in preview"});return;
     case "await-condition":trace.Add(new{nodeId=id,node.Kind,outcome="Business state wait planned; no time elapses or branch executes in preview"});return;
     case "end":case "return":trace.Add(new{nodeId=id,node.Kind,outcome="End"});return;
     case "condition":var match=await expressions.EvaluateAsync(node.Predicate??new(node.Field,node.Operator,node.Value),ctx,graph.EntityKey,permissions,ct);detail=match;next=match?node.IfTrue:node.IfFalse;break;
     case "variable":ctx["variables"]![node.OutputVariable!]=await expressions.ResolveAsync(node.Value,ctx,graph.EntityKey,permissions,ct);detail=ctx["variables"]![node.OutputVariable!]!.DeepClone();break;
     case "query":var source=expressions.Source(node.QueryEntity!,permissions);var predicate=node.Predicate==null?null:await expressions.ResolvePredicateAsync(node.Predicate,ctx,graph.EntityKey,permissions,ct);var page=await source.QueryAsync(predicate,node.Limit,ct);if(page.Total>node.Limit)throw new ValidationException("Query exceeds its limit.");ctx["variables"]![node.OutputVariable!]=new JsonArray(page.Items.Select(x=>(JsonNode)WorkflowExpressions.ToJson(x,source.Metadata)).ToArray());detail=new{page.Total,node.OutputVariable};break;
     case "loop":var items=await expressions.ResolveAsync("{{"+node.ItemsVariable+"}}",ctx,graph.EntityKey,permissions,ct) as JsonArray??throw new ValidationException("Loop requires a list.");var old=ctx["item"]?.DeepClone();foreach(var item in items){ctx["item"]=item?.DeepClone();await Walk(node.Body!,ctx,depth+1);}ctx["item"]=old;detail=new{items=items.Count};break;
     case "parallel":var branches=new JsonObject();foreach(var child in node.Branches!){var clone=(JsonObject)ctx.DeepClone();await Walk(child,clone,depth+1,true);branches[child]=clone["variables"]?.DeepClone();}ctx["branches"]=branches;detail="All branches previewed";break;
     case "join":if(branch){trace.Add(new{nodeId=id,node.Kind,outcome="Branch joined"});return;}detail="Joined";break;
     case "wait":detail=new{plannedDelayMinutes=node.DelayMinutes,plannedUntil=node.WaitUntil==null?null:WorkflowExpressions.Text(await expressions.ResolveAsync(node.WaitUntil,ctx,graph.EntityKey,permissions,ct))};break;
     case "subworkflow":detail=new{plannedVersionId=node.WorkflowVersionId,recordId=node.Value==null?request.RecordId.ToString():WorkflowExpressions.Text(await expressions.ResolveAsync(node.Value,ctx,graph.EntityKey,permissions,ct)),note="Child workflow execution is not performed in preview"};break;
     case "action":var input=new Dictionary<string,string>();foreach(var pair in node.Inputs??new())input[pair.Key]=WorkflowExpressions.Text(await expressions.ResolveAsync(pair.Value,ctx,graph.EntityKey,permissions,ct));var handler=handlers.Single(x=>x.Descriptor.Key==node.ActionKey);await handler.ValidateAsync(input,ct);var output=new JsonObject();if(handler is WorkflowInspectionAction inspection){var inspected=await inspection.ExecuteDetailedAsync(new(Guid.Empty,node.Id,Guid.Empty,Guid.Empty,WorkflowExpressions.Text(ctx["record"]?["label"]),request.RecordId),input,ct);foreach(var pair in inspected.Outputs)output[pair.Key]=JsonValue.Create(pair.Value);detail=new{inspection=node.ActionKey,inputs=input,outputs=inspected.Outputs};}else{detail=new{plannedAction=node.ActionKey,inputs=input};foreach(var key in handler.Descriptor.Outputs??[])output[key]=key.EndsWith("Id")?Guid.Empty.ToString():"preview";}ctx["steps"]![node.Id]=output;break;
    }
    trace.Add(new{nodeId=id,node.Kind,outcome=detail});id=next??throw new ValidationException("Next node missing.");
   }
  }
  await Walk(graph.EntryNodeId,context,0);return new{mode="preview",mutations=0,deliveries=0,trace,variables=context["variables"],note="Read-only inspections use current data. Mutating actions are planned only. Generated IDs and external outputs are placeholders; waits are not elapsed."};
 }
}
