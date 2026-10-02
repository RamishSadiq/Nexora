using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Reporting;
namespace Nexora.Modules.Workflow;
public sealed class WorkflowExpressions(IEnumerable<IWorkflowEntitySource> sources,IEnumerable<IReportDataset> datasets)
{
 public static readonly string[] Operators=["equals","not-equals","contains","not-contains","greater-than","less-than","greater-or-equal","less-or-equal","is-empty","is-not-empty","in","not-in","changed","changed-from","changed-to"];
 private static readonly Regex Template=new(@"\{\{(.+?)\}\}",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
 private static readonly Regex Path=new(@"^[A-Za-z0-9_.-]{1,250}$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
 public IWorkflowEntitySource Source(string key,string[] permissions)=>sources.SingleOrDefault(x=>x.Metadata.Key==key&&permissions.Contains(x.Metadata.Permission))??throw new ValidationException("The entity is unavailable or access was revoked.");
 public WorkflowEntityMetadata? Metadata(string key)=>sources.SingleOrDefault(x=>x.Metadata.Key==key)?.Metadata;
 public static JsonObject Object(string json)=>JsonNode.Parse(json) as JsonObject??new();
 public static JsonNode? Literal(string? value)
 {
  if(value==null)return null;
  if(value.StartsWith('[')||value.StartsWith('{')){try{return JsonNode.Parse(value);}catch(JsonException){throw new ValidationException("Enter valid JSON for a structured value.");}}
  return JsonValue.Create(value);
 }
 public async Task<JsonObject> ReadRecordAsync(string key,Guid id,string[] permissions,CancellationToken ct)
 {
  var source=sources.SingleOrDefault(x=>x.Metadata.Key==key&&permissions.Contains(x.Metadata.Permission));
  if(source==null){var dataset=WorkflowModule.Dataset(key,datasets,permissions);var row=await dataset.FindAsync(id,ct)??throw new ValidationException("The target record is unavailable.");return new(){["id"]=row.Id.ToString(),["label"]=row.Label,["status"]=row.Status,["createdAtUtc"]=row.CreatedAtUtc.ToString("O")};}
  var record=await source.FindAsync(id,ct)??throw new ValidationException("The target record is unavailable.");return ToJson(record,source.Metadata);
 }
 public static JsonObject ToJson(WorkflowRecord record,WorkflowEntityMetadata metadata)
 {
  var obj=new JsonObject();foreach(var (key,value) in record.Values){var type=metadata.Fields.FirstOrDefault(x=>x.Key==key)?.Type;obj[key]=value==null?null:type=="number"&&decimal.TryParse(value,NumberStyles.Number,CultureInfo.InvariantCulture,out var number)?JsonValue.Create(number):type=="boolean"&&bool.TryParse(value,out var boolean)?JsonValue.Create(boolean):JsonValue.Create(value);}return obj;
 }
 public static string Text(JsonNode? node)=>node is JsonValue v&&v.TryGetValue<string>(out var s)?s??"":node?.ToJsonString()??"";
 public static void CheckExpression(string? value)
 {
  if(value?.Length>16000)throw new ValidationException("A value exceeds 16,000 characters.");
  foreach(Match match in Template.Matches(value??""))CheckTerm(match.Groups[1].Value.Trim(),0);
 }
 private static void CheckTerm(string term,int depth)
 {
  if(depth>8)throw new ValidationException("Expression nesting exceeds 8 levels.");
  if(term.Length==0||term.Length>1000)throw new ValidationException("Expression is empty or too long.");
  if(term.StartsWith('"')){try{JsonSerializer.Deserialize<string>(term);return;}catch{throw new ValidationException("Expression string is invalid.");}}
  if(decimal.TryParse(term,NumberStyles.Number,CultureInfo.InvariantCulture,out _))return;
  var open=term.IndexOf('(');
  if(open>=0){var name=term[..open];if(!term.EndsWith(')')||name is not ("addDays" or "add" or "subtract" or "multiply" or "divide" or "concat" or "coalesce" or "count"))throw new ValidationException("Expression function is unsupported.");foreach(var arg in Arguments(term[(open+1)..^1]))CheckTerm(arg,depth+1);return;}
  if(!Path.IsMatch(term)||term.Split('.')[0] is not ("record" or "related" or "previous" or "trigger" or "actor" or "owner" or "now" or "variables" or "steps" or "item" or "branches"))throw new ValidationException("Use a registered record, variable, output or approved function.");
 }
 private static string[] Arguments(string input)
 {
  var args=new List<string>();var start=0;var depth=0;var quoted=false;var escape=false;
  for(var i=0;i<input.Length;i++){var c=input[i];if(escape){escape=false;continue;}if(c=='\\'&&quoted){escape=true;continue;}if(c=='"'){quoted=!quoted;continue;}if(quoted)continue;if(c=='(')depth++;if(c==')')depth--;if(c==','&&depth==0){args.Add(input[start..i].Trim());start=i+1;}}
  if(quoted||depth!=0)throw new ValidationException("Expression brackets or quotes are unbalanced.");args.Add(input[start..].Trim());if(args.Count>20)throw new ValidationException("Too many expression arguments.");return args.ToArray();
 }
 public async Task<JsonNode?> ResolveAsync(string? value,JsonObject context,string entityKey,string[] permissions,CancellationToken ct)
 {
  CheckExpression(value);if(value==null)return null;var matches=Template.Matches(value);if(matches.Count==1&&matches[0].Value==value)return (await TermAsync(matches[0].Groups[1].Value.Trim(),context,entityKey,permissions,ct,0))?.DeepClone();
  var result=value;foreach(Match match in matches){var resolved=await TermAsync(match.Groups[1].Value.Trim(),context,entityKey,permissions,ct,0);result=result.Replace(match.Value,Text(resolved));}return Literal(result);
 }
 private async Task<JsonNode?> TermAsync(string term,JsonObject context,string key,string[] permissions,CancellationToken ct,int depth)
 {
  if(depth>8)throw new ValidationException("Expression nesting exceeded.");
  if(term.StartsWith('"'))return JsonValue.Create(JsonSerializer.Deserialize<string>(term));
  if(decimal.TryParse(term,NumberStyles.Number,CultureInfo.InvariantCulture,out var number))return JsonValue.Create(number);
  var open=term.IndexOf('(');
  if(open>=0)
  {
   var name=term[..open];var values=new List<JsonNode?>();foreach(var arg in Arguments(term[(open+1)..^1]))values.Add(await TermAsync(arg,context,key,permissions,ct,depth+1));
   decimal Num(int index){if(index>=values.Count||!decimal.TryParse(Text(values[index]),NumberStyles.Number,CultureInfo.InvariantCulture,out var n))throw new ValidationException("Expression expects a number.");return n;}
   if(name=="concat")return JsonValue.Create(string.Concat(values.Select(Text)));
   if(name=="coalesce")return values.FirstOrDefault(v=>!string.IsNullOrEmpty(Text(v)))?.DeepClone();
   if(name=="count")return JsonValue.Create(values.FirstOrDefault() is JsonArray array?array.Count:throw new ValidationException("Count expects a list."));
   if(values.Count!=2)throw new ValidationException("Calculation requires two arguments.");
   if(name=="addDays"){if(!DateTimeOffset.TryParse(Text(values[0]),CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var date)||Math.Abs(Num(1))>36500)throw new ValidationException("Date calculation is invalid.");return JsonValue.Create(date.AddDays((double)Num(1)).ToUniversalTime().ToString("O"));}
   return JsonValue.Create(name switch{"add"=>Num(0)+Num(1),"subtract"=>Num(0)-Num(1),"multiply"=>Num(0)*Num(1),"divide"=>Num(1)==0?throw new ValidationException("Cannot divide by zero."):Num(0)/Num(1),_=>throw new ValidationException("Unknown function.")});
  }
  if(term.StartsWith("record.related."))term=term[7..];
  if(term.StartsWith("related."))
  {
   var parts=term.Split('.');if(parts.Length<3||parts.Length>8)throw new ValidationException("Related field path is invalid.");
   var current=context["record"] as JsonObject??throw new ValidationException("Current record is unavailable.");var entity=key;
   for(var i=1;i<parts.Length-1;i++)
   {
    var relation=Source(entity,permissions).Metadata.Relations.SingleOrDefault(r=>r.Key==parts[i])??throw new ValidationException("Related entity is unavailable.");
    var source=Source(relation.EntityKey,permissions);var id=Text(current[relation.SourceField]);if(string.IsNullOrEmpty(id))return null;
    var page=await source.QueryAsync(new(Field:relation.TargetField,Operator:"equals",Value:id),relation.Many?100:1,ct);if(page.Items.Count==0)return null;if(relation.Many)throw new ValidationException("Use a query node for a collection relationship.");current=ToJson(page.Items[0],source.Metadata);entity=relation.EntityKey;
   }
   return current[parts[^1]]?.DeepClone();
  }
  JsonNode? node=context;foreach(var part in term.Split('.')){if(node is JsonObject obj){if(!obj.ContainsKey(part))throw new ValidationException("A referenced value is unavailable: "+term);node=obj[part];}else if(node is JsonArray list&&int.TryParse(part,out var index)&&index>=0&&index<list.Count)node=list[index];else throw new ValidationException("A referenced value is unavailable: "+term);}return node?.DeepClone();
 }
 public async Task<bool> EvaluateAsync(WorkflowPredicate predicate,JsonObject context,string key,string[] permissions,CancellationToken ct,int depth=0)
 {
  if(depth>8)throw new ValidationException("Condition nesting exceeds the limit.");
  if(predicate.All!=null){foreach(var p in predicate.All)if(!await EvaluateAsync(p,context,key,permissions,ct,depth+1))return false;return true;}
  if(predicate.Any!=null){foreach(var p in predicate.Any)if(await EvaluateAsync(p,context,key,permissions,ct,depth+1))return true;return false;}
  var field=predicate.Field??throw new ValidationException("Condition field is missing.");var path=field.StartsWith("record.")||field.StartsWith("variables.")||field.StartsWith("item.")||field.StartsWith("steps.")?field:"record."+field;
  var actual=await ResolveAsync("{{"+path+"}}",context,key,permissions,ct);var desired=await ResolveAsync(predicate.Value,context,key,permissions,ct);var text=Text(actual);var wanted=Text(desired);
  if(predicate.Operator is "changed" or "changed-from" or "changed-to")
  {
   var originalKey=field.StartsWith("record.")?field[7..]:field;var previous=context["previous"] as JsonObject;if(previous==null||!previous.ContainsKey(originalKey))throw new ValidationException("Previous values are unavailable for this trigger.");var old=Text(previous[originalKey]);var changed=!string.Equals(old,text,StringComparison.Ordinal);return predicate.Operator=="changed"?changed:changed&&string.Equals(predicate.Operator=="changed-from"?old:text,wanted,StringComparison.OrdinalIgnoreCase);
  }
  int Compare(){if(decimal.TryParse(text,NumberStyles.Number,CultureInfo.InvariantCulture,out var a)&&decimal.TryParse(wanted,NumberStyles.Number,CultureInfo.InvariantCulture,out var b))return a.CompareTo(b);if(DateTimeOffset.TryParse(text,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var da)&&DateTimeOffset.TryParse(wanted,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var db))return da.CompareTo(db);return string.Compare(text,wanted,StringComparison.OrdinalIgnoreCase);}
  return predicate.Operator switch{"equals"=>string.Equals(text,wanted,StringComparison.OrdinalIgnoreCase),"not-equals"=>!string.Equals(text,wanted,StringComparison.OrdinalIgnoreCase),"contains"=>text.Contains(wanted,StringComparison.OrdinalIgnoreCase),"not-contains"=>!text.Contains(wanted,StringComparison.OrdinalIgnoreCase),"is-empty"=>string.IsNullOrWhiteSpace(text),"is-not-empty"=>!string.IsNullOrWhiteSpace(text),"in"=>wanted.Split(',',StringSplitOptions.TrimEntries).Contains(text,StringComparer.OrdinalIgnoreCase),"not-in"=>!wanted.Split(',',StringSplitOptions.TrimEntries).Contains(text,StringComparer.OrdinalIgnoreCase),"greater-than"=>Compare()>0,"less-than"=>Compare()<0,"greater-or-equal"=>Compare()>=0,"less-or-equal"=>Compare()<=0,_=>throw new ValidationException("Condition operator is unsupported.")};
 }
 public async Task<WorkflowPredicate> ResolvePredicateAsync(WorkflowPredicate p,JsonObject context,string key,string[] permissions,CancellationToken ct)
 {
  if(p.All!=null){var list=new List<WorkflowPredicate>();foreach(var child in p.All)list.Add(await ResolvePredicateAsync(child,context,key,permissions,ct));return p with{All=list.ToArray()};}
  if(p.Any!=null){var list=new List<WorkflowPredicate>();foreach(var child in p.Any)list.Add(await ResolvePredicateAsync(child,context,key,permissions,ct));return p with{Any=list.ToArray()};}
  return p with{Value=Text(await ResolveAsync(p.Value,context,key,permissions,ct))};
 }
 public static string BoundedJson(JsonNode node){var json=node.ToJsonString();if(json.Length>262144)throw new ValidationException("Workflow context is too large. Narrow the query or outputs.");return json;}
}
