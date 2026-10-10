using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Reporting;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Workflow;
public sealed class WorkflowValidation(IEnumerable<IReportDataset> datasets,IEnumerable<IWorkflowEntitySource> sources,IEnumerable<IWorkflowActionHandler> handlers)
{
 public static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
 public static readonly string[] NodeKinds=["condition","action","wait","query","variable","loop","return","parallel","join","subworkflow","approval","await-condition","end"];
 public static readonly string[] Triggers=["manual","record.created","record.updated","record.deleted","record.status-changed","record.field-changed","record.assigned","scheduled","external"];
 public static string Serialize(WorkflowGraph graph)=>JsonSerializer.Serialize(graph,Json);
 public static WorkflowGraph Parse(string json)=>JsonSerializer.Deserialize<WorkflowGraph>(json,Json)??throw new ValidationException("Invalid workflow definition.");
 public static string[] Structure(WorkflowGraph? graph)
 {
  if(graph==null)return ["Provide a workflow definition."];
  if(graph.SchemaVersion is not (1 or 2))return ["Unsupported definition version."];
  if(graph.Nodes is not {Length:>0 and <=100}||graph.Nodes.Any(x=>x==null))return ["Provide 1–100 nodes."];
  if(Serialize(graph).Length>262144)return ["Workflow is too large."];
  if(graph.Nodes.Any(x=>string.IsNullOrWhiteSpace(x.Id)||x.Id.Length>80)||graph.Nodes.Select(x=>x.Id).Distinct().Count()!=graph.Nodes.Length)return ["Node identifiers must be unique and at most 80 characters."];
  if(graph.Nodes.Any(n=>n.Kind==null||n.Label==null||n.Label.Length>160||n.Inputs?.Count>50||n.Inputs?.Any(p=>p.Value==null||p.Value.Length>16000)==true))return ["Node values are invalid or too long."];
  if(graph.Variables?.Any(p=>p.Value==null||p.Value.Length>16000)==true)return ["Variable values are invalid or too long."];
  if(graph.Variables?.Count>50)return ["At most 50 initial variables are allowed."];
  if(graph.RequiredVariables?.Any(x=>!VariableName(x))==true)return ["Required configuration names must be valid variable names."];
  return [];
 }
 public async Task<string[]> ValidateAsync(WorkflowGraph graph,string[] permissions,CancellationToken ct,bool checkLiteralValues=true)
 {
  var structural=Structure(graph);if(structural.Length>0)return structural;var errors=new List<string>();
  if(checkLiteralValues)foreach(var key in graph.RequiredVariables??[])if(string.IsNullOrWhiteSpace(graph.Variables?.GetValueOrDefault(key)))errors.Add("Configure required variable: "+key);
  var entity=sources.SingleOrDefault(x=>x.Metadata.Key==graph.EntityKey&&permissions.Contains(x.Metadata.Permission));
  if(entity==null&&!datasets.Any(x=>x.Key==graph.EntityKey&&permissions.Contains(x.Permission)))errors.Add("Choose an accessible entity.");
  if(!Triggers.Contains(graph.Trigger))errors.Add("Trigger type is unsupported.");
  if(graph.Trigger.StartsWith("record.")&&entity==null)errors.Add("Automatic events require a registered entity source.");
  if(graph.Trigger=="record.field-changed"&&!(entity?.Metadata.Fields.Any(f=>f.Key==graph.TriggerField)??false))errors.Add("Choose a trigger field.");
  if(graph.Trigger=="scheduled")try{ValidateSchedule(graph.Schedule,entity?.Metadata);}catch(ValidationException e){errors.Add(e.Message);}
  if(graph.Filter!=null)ValidatePredicate(graph.Filter,entity?.Metadata,errors,"trigger",permissions);
  if(graph.Variables!=null)foreach(var (name,value) in graph.Variables){if(!VariableName(name))errors.Add("Variable names must use letters, numbers and underscores.");try{WorkflowExpressions.CheckExpression(value);}catch(ValidationException e){errors.Add(e.Message);}}
  var ids=graph.Nodes.Select(x=>x.Id).ToHashSet();if(!ids.Contains(graph.EntryNodeId))errors.Add("Choose an entry node.");
  foreach(var n in graph.Nodes)
  {
   var prefix=n.Label??n.Id;
   if(string.IsNullOrWhiteSpace(n.Label)||n.Label.Length>160)errors.Add($"{n.Id}: enter a label of 1–160 characters.");
   if(!NodeKinds.Contains(n.Kind)){errors.Add($"{prefix}: unsupported node type.");continue;}
   if(n.OnError is not ("stop" or "retry" or "skip" or "continue" or "escalate"))errors.Add($"{prefix}: unsupported failure policy.");
   if(n.Kind=="condition"&&n.OnError is "skip" or "continue"&&n.ErrorNext==null)errors.Add($"{prefix}: choose an error destination for a condition failure.");
   if(n.OnError=="escalate"&&(n.ErrorNext==null||!ids.Contains(n.ErrorNext)))errors.Add($"{prefix}: escalation requires an error destination.");
   if(n.Kind=="condition")ValidatePredicate(n.Predicate??new(n.Field,n.Operator,n.Value),entity?.Metadata,errors,prefix,permissions);
   if(n.Kind=="action")
   {
    var handler=handlers.SingleOrDefault(x=>x.Descriptor.Key==n.ActionKey);
    if(handler==null)errors.Add($"{prefix}: unsupported action.");
    else if(!handler.Descriptor.RequiredPermissions.All(permissions.Contains))errors.Add($"{prefix}: action permission is missing.");
    else
    {
     var inputs=n.Inputs??new();if(inputs.Keys.Any(k=>!handler.Descriptor.Inputs.Contains(k)))errors.Add($"{prefix}: unsupported action input.");
     foreach(var required in handler.Descriptor.InputSchema?.Where(x=>x.Required)??[])if(string.IsNullOrWhiteSpace(inputs.GetValueOrDefault(required.Key)))errors.Add($"{prefix}: {required.Title} is required.");
     foreach(var (key,value) in inputs)try{WorkflowExpressions.CheckExpression(value);var field=handler.Descriptor.InputSchema?.FirstOrDefault(x=>x.Key==key);if(value.Contains("{{"))continue;if(field?.Choices!=null&&!string.IsNullOrWhiteSpace(value)&&!field.Choices.Contains(value))throw new ValidationException("Choose a supported value for "+field.Title);if(field?.Type=="number"&&!string.IsNullOrWhiteSpace(value)&&!decimal.TryParse(value,NumberStyles.Number,CultureInfo.InvariantCulture,out _))throw new ValidationException("Enter a number for "+field.Title);}catch(ValidationException e){errors.Add($"{prefix}: {e.Message}");}
     // Definition values can refer to records/outputs that will exist only at execution.
     if(checkLiteralValues&&!inputs.Values.Any(v=>v.Contains("{{")))try{await handler.ValidateAsync(inputs,ct);}catch(ValidationException e){errors.Add($"{prefix}: {e.Message}");}
    }
   }
   if(n.Kind=="wait")
   {
    if(n.WaitUntil==null&&n.DelayMinutes is not (>0 and <=525600))errors.Add($"{prefix}: delay must be 1–525600 minutes.");
    if(n.WaitUntil!=null)try{WorkflowExpressions.CheckExpression(n.WaitUntil);}catch(ValidationException e){errors.Add($"{prefix}: {e.Message}");}
   }
   if(n.Kind=="query")
   {
    var source=sources.SingleOrDefault(s=>s.Metadata.Key==n.QueryEntity&&permissions.Contains(s.Metadata.Permission));if(source==null)errors.Add($"{prefix}: query entity is unavailable.");
    if(n.Predicate!=null)ValidatePredicate(n.Predicate,source?.Metadata,errors,prefix,permissions,query:true);
    if(n.Limit is <1 or >1000)errors.Add($"{prefix}: query limit must be 1–1000.");if(!VariableName(n.OutputVariable))errors.Add($"{prefix}: name the query output variable.");
   }
   if(n.Kind=="variable"){if(!VariableName(n.OutputVariable))errors.Add($"{prefix}: name the variable.");try{WorkflowExpressions.CheckExpression(n.Value);}catch(ValidationException e){errors.Add($"{prefix}: {e.Message}");}}
   if(n.Kind=="loop"&&(string.IsNullOrWhiteSpace(n.ItemsVariable)||!ids.Contains(n.Body??"")))errors.Add($"{prefix}: choose a list and a loop body.");
   if(n.Kind=="parallel"&&(n.Branches is not {Length:>=2 and <=8}||n.Branches.Any(b=>!ids.Contains(b))||graph.Nodes.FirstOrDefault(x=>x.Id==n.Next)?.Kind!="join"))errors.Add($"{prefix}: choose 2–8 branches and a join node.");
   if(n.Kind=="subworkflow"&&!Guid.TryParse(n.WorkflowVersionId,out _))errors.Add($"{prefix}: choose a published sub-workflow version.");
   if(n.Kind is "approval" or "await-condition")
   {
    if(n.TimeoutMinutes is <1 or >525600||n.PollMinutes is <1 or >1440)errors.Add($"{prefix}: choose a timeout of 1–525600 minutes and recheck interval of 1–1440 minutes.");
    if(n.TimeoutNext==null)errors.Add($"{prefix}: choose a timeout destination.");
    if(n.Kind=="approval")
    {
     if(string.IsNullOrWhiteSpace(n.ApproverUserId)&&string.IsNullOrWhiteSpace(n.ApproverTeamId))errors.Add($"{prefix}: choose an approval user or team.");
     if(n.RequiredPermission!=null&&n.RequiredPermission!=""&&!WorkflowGates.DecisionPermissions.Contains(n.RequiredPermission))errors.Add($"{prefix}: unsupported decision permission.");
     foreach(var value in new[]{n.ApproverUserId,n.ApproverTeamId})try{WorkflowExpressions.CheckExpression(value);}catch(ValidationException e){errors.Add(prefix+": "+e.Message);}
    }
    else
    {
     if(n.ActionKey!=null){var inspection=handlers.SingleOrDefault(h=>h.Descriptor.Key==n.ActionKey);if(inspection is not WorkflowInspectionAction)errors.Add($"{prefix}: business waits require a read-only inspection.");else if(!inspection.Descriptor.RequiredPermissions.All(permissions.Contains))errors.Add($"{prefix}: inspection permission is missing.");else{foreach(var input in inspection.Descriptor.InputSchema??[])if(input.Required&&string.IsNullOrWhiteSpace(n.Inputs?.GetValueOrDefault(input.Key)))errors.Add($"{prefix}: {input.Title} is required.");foreach(var value in n.Inputs?.Values.AsEnumerable()??[])try{WorkflowExpressions.CheckExpression(value);}catch(ValidationException e){errors.Add(prefix+": "+e.Message);}}}
     var waitMetadata=n.QueryEntity==null?entity?.Metadata:sources.SingleOrDefault(s=>s.Metadata.Key==n.QueryEntity&&permissions.Contains(s.Metadata.Permission))?.Metadata;
     if(waitMetadata==null)errors.Add($"{prefix}: wait entity is unavailable.");
     if(n.Predicate==null)errors.Add($"{prefix}: configure a business-state condition.");else ValidatePredicate(n.Predicate,waitMetadata,errors,prefix,permissions);
     if(n.QueryEntity!=null&&string.IsNullOrWhiteSpace(n.Value))errors.Add($"{prefix}: choose the related wait target.");
     try{WorkflowExpressions.CheckExpression(n.Value);}catch(ValidationException e){errors.Add(prefix+": "+e.Message);}
    }
   }
   foreach(var target in Targets(n))if(string.IsNullOrEmpty(target)||!ids.Contains(target))errors.Add($"{prefix}: choose an existing destination.");
  }
  var seen=new HashSet<string>();var visiting=new HashSet<string>();var cycle=false;
  void Visit(string id){if(visiting.Contains(id)){cycle=true;return;}if(!seen.Add(id))return;visiting.Add(id);foreach(var target in Targets(graph.Nodes.Single(x=>x.Id==id)))if(target!=null&&ids.Contains(target))Visit(target);visiting.Remove(id);}
  if(ids.Contains(graph.EntryNodeId))Visit(graph.EntryNodeId);
  if(cycle)errors.Add("Ordinary cycles are not supported. Use a bounded loop node.");
  if(seen.Count!=ids.Count)errors.Add("All nodes must be reachable from the entry node.");
  return errors.Distinct().ToArray();
 }
 public static bool VariableName(string? name)=>name!=null&&Regex.IsMatch(name,@"^[A-Za-z_][A-Za-z0-9_]{0,79}$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
 private void ValidatePredicate(WorkflowPredicate predicate,WorkflowEntityMetadata? metadata,List<string> errors,string prefix,string[] permissions,int depth=0,bool query=false)
 {
  if(depth>8){errors.Add(prefix+": condition groups exceed 8 levels.");return;}
  if(predicate.All!=null||predicate.Any!=null)
  {
   if(predicate.All!=null&&predicate.Any!=null){errors.Add(prefix+": choose one group operator.");return;}var group=predicate.All??predicate.Any!;if(group.Length is <1 or >50){errors.Add(prefix+": groups require 1–50 conditions.");return;}foreach(var child in group){if(child==null)errors.Add(prefix+": condition is missing.");else ValidatePredicate(child,metadata,errors,prefix,permissions,depth+1,query);}return;
  }
  if(!WorkflowExpressions.Operators.Contains(predicate.Operator??""))errors.Add(prefix+": unsupported operator.");
  var field=predicate.Field??"";if(field.StartsWith("record."))field=field[7..];
  if(field.StartsWith("related."))
  {
   var parts=field.Split('.');var current=metadata;for(var i=1;i<parts.Length-1;i++){var relation=current?.Relations.SingleOrDefault(r=>r.Key==parts[i]);current=relation==null?null:sources.SingleOrDefault(s=>s.Metadata.Key==relation.EntityKey&&permissions.Contains(s.Metadata.Permission))?.Metadata;}if(current==null||!current.Fields.Any(f=>f.Key==parts[^1])||query)errors.Add(prefix+": related field is unavailable for this operation.");
  }
  else if(!field.StartsWith("variables.")&&!field.StartsWith("steps.")&&!field.StartsWith("item.")&&!(metadata?.Fields.Any(f=>f.Key==field)??field is "label" or "status" or "createdAtUtc"))errors.Add(prefix+": field is unavailable.");
  var fieldType=metadata?.Fields.FirstOrDefault(f=>f.Key==field)?.Type;if(predicate.Operator is "greater-than" or "less-than" or "greater-or-equal" or "less-or-equal"&&fieldType is not (null or "number" or "date"))errors.Add(prefix+": ordering requires a numeric or date field.");
  if(query&&(field.Contains('.')||predicate.Operator?.StartsWith("changed")==true))errors.Add(prefix+": query conditions require direct entity fields.");
  try{WorkflowExpressions.CheckExpression(predicate.Value);}catch(ValidationException e){errors.Add(prefix+": "+e.Message);}
 }
 public static void ValidateSchedule(WorkflowScheduleConfig? schedule,WorkflowEntityMetadata? metadata)
 {
  if(schedule==null)throw new ValidationException("Configure the schedule.");
  if(schedule.Mode is not ("interval" or "daily" or "weekly" or "once" or "relative"))throw new ValidationException("Schedule type is unsupported.");
  if(schedule.IntervalMinutes is <1 or >525600)throw new ValidationException("Schedule interval must be 1–525600 minutes.");
  if(schedule.Mode is "daily" or "weekly"&&!TimeOnly.TryParseExact(schedule.Time,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out _))throw new ValidationException("Use HH:mm for schedule time.");
  try{TimeZoneInfo.FindSystemTimeZoneById(schedule.Timezone);}catch{throw new ValidationException("Choose a valid timezone.");}
  if(schedule.Weekday is <0 or >6)throw new ValidationException("Weekday must be 0–6.");
  if(schedule.Mode=="once"&&schedule.AtUtc==null)throw new ValidationException("Choose a schedule date/time.");
  if(schedule.Mode=="relative"&&!(metadata?.Fields.Any(f=>f.Key==schedule.AnchorField&&f.Type=="date")??false))throw new ValidationException("Choose an entity date field for the relative schedule.");
  if(Math.Abs(schedule.OffsetDays)>36500)throw new ValidationException("Schedule offset is too large.");
 }
 public static IEnumerable<string?> Targets(WorkflowNode n)
 {
  IEnumerable<string?> targets=n.Kind switch{"end" or "return"=>[],"condition" or "approval"=>[n.IfTrue,n.IfFalse],"loop"=>[n.Body,n.Next],"parallel"=>[..n.Branches??[],n.Next],_=>[n.Next]};if(n.TimeoutNext!=null)targets=targets.Append(n.TimeoutNext);return n.ErrorNext==null?targets:targets.Append(n.ErrorNext);
 }
 public static bool Evaluate(WorkflowNode n,ReportRow row)=>n.Operator switch{"equals"=>string.Equals(n.Field=="status"?row.Status:row.Label,n.Value,StringComparison.OrdinalIgnoreCase),"not-equals"=>!string.Equals(n.Field=="status"?row.Status:row.Label,n.Value,StringComparison.OrdinalIgnoreCase),"contains"=>(n.Field=="status"?row.Status:row.Label).Contains(n.Value??"",StringComparison.OrdinalIgnoreCase),"is-empty"=>string.IsNullOrWhiteSpace(n.Field=="status"?row.Status:row.Label),"is-not-empty"=>!string.IsNullOrWhiteSpace(n.Field=="status"?row.Status:row.Label),_=>throw new ValidationException("Unsupported condition.")};
}
