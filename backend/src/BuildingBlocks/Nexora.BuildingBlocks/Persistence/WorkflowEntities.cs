using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Domain;
using Nexora.BuildingBlocks.Security;
namespace Nexora.BuildingBlocks.Persistence;

public interface IWorkflowWriteIdentity { IRequestIdentity WorkflowIdentity { get; } }
public sealed record WorkflowPredicate(string? Field=null,string? Operator=null,string? Value=null,WorkflowPredicate[]? All=null,WorkflowPredicate[]? Any=null);
public sealed record WorkflowField(string Key,string Title,string Type);
public sealed record WorkflowRelation(string Key,string EntityKey,string SourceField,string TargetField="id",bool Many=false);
public sealed record WorkflowEntityMetadata(string Key,string Title,string Permission,WorkflowField[] Fields,WorkflowRelation[] Relations);
public sealed record WorkflowRecord(Guid Id,string Label,Dictionary<string,string?> Values);
public sealed record WorkflowRecordPage(int Total,IReadOnlyList<WorkflowRecord> Items);
public interface IWorkflowEntitySource
{
 WorkflowEntityMetadata Metadata{get;}
 Task<WorkflowRecord?> FindAsync(Guid id,CancellationToken ct);
 Task<WorkflowRecordPage> QueryAsync(WorkflowPredicate? predicate,int take,CancellationToken ct);
 Task<WorkflowDomainEvent[]> PendingEventsAsync(CancellationToken ct);
 Task AcknowledgeEventAsync(Guid tenantId,Guid id,CancellationToken ct);
}
public sealed record WorkflowEntityRegistration(string Key,string Title,string Permission,Type RecordType,string LabelProperty,string? DiscriminatorProperty,string? DiscriminatorValue,WorkflowRelation[] Relations)
{
 public PropertyInfo[] Properties=>EntityFieldCatalog.Properties(RecordType);
 public static string KeyOf(string name)=>char.ToLowerInvariant(name[0])+name[1..];
 public static string? Format(object? value)=>value switch{null=>null,DateOnly d=>d.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),DateTime d=>DateTime.SpecifyKind(d,DateTimeKind.Utc).ToString("O"),DateTimeOffset d=>d.ToUniversalTime().ToString("O"),bool b=>b?"true":"false",IFormattable f=>f.ToString(null,CultureInfo.InvariantCulture),_=>value.ToString()};
 public Dictionary<string,string?> Snapshot(PropertyValues values)
 {
  var result=Properties.ToDictionary(p=>KeyOf(p.Name),p=>Format(values[p.Name]));result["label"]=Format(values[LabelProperty]);return result;
 }
 public WorkflowEntityMetadata Metadata=>new(Key,Title,Permission,[new("label","Name","text"),..Properties.Where(p=>KeyOf(p.Name)!="label").Select(p=>new WorkflowField(KeyOf(p.Name),EntityFieldCatalog.Label(p.Name),EntityFieldCatalog.DataType(p.PropertyType)!))],Relations);
}
public static class WorkflowEntityServices
{
 public static IServiceCollection AddWorkflowEntity<TDb,T>(this IServiceCollection services,string key,string title,string permission,string labelProperty,Expression<Func<T,bool>>? filter=null,string? discriminatorProperty=null,string? discriminatorValue=null,params WorkflowRelation[] relations) where TDb:DbContext where T:class
 {
  var registration=new WorkflowEntityRegistration(key,title,permission,typeof(T),labelProperty,discriminatorProperty,discriminatorValue,relations);
  services.AddSingleton(registration);services.AddScoped<IWorkflowEntitySource>(sp=>new WorkflowEntitySource<TDb,T>(sp.GetRequiredService<TDb>(),registration,filter));return services;
 }
}
public sealed class WorkflowEntitySource<TDb,T>(TDb db,WorkflowEntityRegistration registration,Expression<Func<T,bool>>? filter):IWorkflowEntitySource where TDb:DbContext where T:class
{
 private IWorkflowAttributes? attributes;
 private bool attributesLoaded;
 private IWorkflowAttributes? Attributes
 {
  get
  {
   if(attributesLoaded)return attributes;
   attributesLoaded=true;
   attributes=WorkflowAttributes.Create(db,registration);
   return attributes;
  }
 }
 public WorkflowEntityMetadata Metadata=>registration.Metadata with { Fields=[..registration.Metadata.Fields,..(Attributes?.Fields??[])] };
 private IQueryable<T> Source=>filter==null?db.Set<T>().AsNoTracking():db.Set<T>().AsNoTracking().Where(filter);
 private WorkflowRecord Map(T row)
 {
  var values=registration.Properties.ToDictionary(p=>WorkflowEntityRegistration.KeyOf(p.Name),p=>WorkflowEntityRegistration.Format(p.GetValue(row)));values["label"]=WorkflowEntityRegistration.Format(registration.RecordType.GetProperty(registration.LabelProperty)!.GetValue(row));return new(Guid.Parse(values["id"]!),values["label"]??"",values);
 }
 public async Task<WorkflowRecord?> FindAsync(Guid id,CancellationToken ct){var row=await Source.SingleOrDefaultAsync(x=>EF.Property<Guid>(x,"Id")==id,ct);return row==null?null:(await EnrichAsync([Map(row)],ct))[0];}
 public async Task<WorkflowRecordPage> QueryAsync(WorkflowPredicate? predicate,int take,CancellationToken ct)
 {
  if(take is <1 or >1000)throw new ValidationException("Query limit must be 1–1000.");var query=Source;
  if(predicate!=null){var parameter=Expression.Parameter(typeof(T),"row");query=query.Where(Expression.Lambda<Func<T,bool>>(Build(predicate,parameter,0),parameter));}
  var total=await query.CountAsync(ct);var rows=await query.OrderBy(x=>EF.Property<Guid>(x,"Id")).Take(take).ToListAsync(ct);return new(total,await EnrichAsync(rows.Select(Map).ToArray(),ct));
 }
 private Task<WorkflowRecord[]> EnrichAsync(WorkflowRecord[] records,CancellationToken ct)=>Attributes?.EnrichAsync(records,ct)??Task.FromResult(records);
 private Expression Build(WorkflowPredicate p,ParameterExpression parameter,int depth)
 {
  if(depth>8)throw new ValidationException("Condition groups exceed 8 levels.");
  if(p.All!=null||p.Any!=null){var group=p.All??p.Any!;if(group.Length is <1 or >50)throw new ValidationException("Condition groups require 1–50 items.");return group.Select(x=>Build(x,parameter,depth+1)).Aggregate((left,right)=>p.All!=null?Expression.AndAlso(left,right):Expression.OrElse(left,right));}
  var property=registration.Properties.SingleOrDefault(x=>WorkflowEntityRegistration.KeyOf(x.Name)==p.Field||p.Field=="label"&&x.Name==registration.LabelProperty);
  var member=property!=null?(Expression)Expression.Property(parameter,property):Attributes?.ValueExpression(p.Field??"",parameter)??throw new ValidationException("Query field is unavailable.");var type=Nullable.GetUnderlyingType(member.Type)??member.Type;Expression isNull=type==member.Type&&type.IsValueType?Expression.Constant(false):Expression.Equal(member,Expression.Constant(null,member.Type));
  if(p.Operator is "is-empty" or "is-not-empty"){Expression empty=isNull;if(type==typeof(string))empty=Expression.OrElse(empty,Expression.Equal(member,Expression.Constant("")));return p.Operator=="is-empty"?empty:Expression.Not(empty);}
  object? ConvertValue(string? value)=>type.IsEnum?Enum.Parse(type,value!,true):type==typeof(DateTimeOffset)?DateTimeOffset.Parse(value!,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal):type==typeof(string)?value??"":type==typeof(Guid)?Guid.Parse(value!):type==typeof(DateOnly)?DateOnly.Parse(value!,CultureInfo.InvariantCulture):type==typeof(DateTime)?DateTime.Parse(value!,CultureInfo.InvariantCulture,DateTimeStyles.AdjustToUniversal|DateTimeStyles.AssumeUniversal):System.Convert.ChangeType(value,type,CultureInfo.InvariantCulture);
  if(p.Operator is "in" or "not-in")
  {
   var items=(p.Value??"").Split(',',StringSplitOptions.TrimEntries);if(items.Length>100)throw new ValidationException("Too many set values.");Expression match=Expression.Constant(false);foreach(var item in items){object? v;try{v=ConvertValue(item);}catch{throw new ValidationException("Set value does not match its field type.");}match=Expression.OrElse(match,Expression.Equal(member,Expression.Convert(Expression.Constant(v,type),member.Type)));}return p.Operator=="in"?match:Expression.Not(match);
  }
  Expression value;
  try{value=Expression.Convert(Expression.Constant(ConvertValue(p.Value),type),member.Type);}catch(Exception){throw new ValidationException("Query value does not match its field type.");}
  if(p.Operator is "contains" or "not-contains"){if(type!=typeof(string))throw new ValidationException("Contains requires a text field.");var contains=Expression.AndAlso(Expression.Not(isNull),Expression.Call(member,typeof(string).GetMethod("Contains",[typeof(string)])!,Expression.Constant(p.Value??"")));return p.Operator=="contains"?contains:Expression.Not(contains);}
  if(type==typeof(string)&&p.Operator is "greater-than" or "less-than" or "greater-or-equal" or "less-or-equal")throw new ValidationException("Ordering requires a date or numeric field.");
  return p.Operator switch{"equals"=>Expression.Equal(member,value),"not-equals"=>Expression.NotEqual(member,value),"greater-than"=>Expression.GreaterThan(member,value),"less-than"=>Expression.LessThan(member,value),"greater-or-equal"=>Expression.GreaterThanOrEqual(member,value),"less-or-equal"=>Expression.LessThanOrEqual(member,value),_=>throw new ValidationException("Unsupported query operator.")};
 }
 // Cross-tenant dispatch methods only discover/acknowledge envelopes; readers always retain module filters.
 public Task<WorkflowDomainEvent[]> PendingEventsAsync(CancellationToken ct)=>db.Set<WorkflowDomainEvent>().IgnoreQueryFilters().AsNoTracking().Where(x=>x.EntityKey==registration.Key&&x.DeliveredAtUtc==null).OrderBy(x=>x.CreatedAtUtc).Take(50).ToArrayAsync(ct);
 public Task AcknowledgeEventAsync(Guid tenantId,Guid id,CancellationToken ct)=>db.Set<WorkflowDomainEvent>().IgnoreQueryFilters().Where(x=>x.Id==id&&x.TenantId==tenantId&&x.EntityKey==registration.Key).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.DeliveredAtUtc,DateTime.UtcNow),ct);
}
public sealed class WorkflowDomainEvent:TenantEntity
{
 public string EntityKey{get;set;}="";public Guid RecordId{get;set;}public Guid ActorId{get;set;}public string Kind{get;set;}="";public string CurrentJson{get;set;}="{}";public string PreviousJson{get;set;}="{}";public string AncestryJson{get;set;}="[]";public Guid? CausationRunId{get;set;}public Guid? RootRunId{get;set;}public DateTime? DeliveredAtUtc{get;set;}
}
public sealed class WorkflowActionReceipt:TenantEntity,IAppendOnly
{
 public Guid RunId{get;set;}public string ActionKey{get;set;}="";public string ResultJson{get;set;}="{}";
}
public static class WorkflowOutboxModel
{
 public static void Configure(ModelBuilder b)
 {
  foreach(var type in new[]{typeof(WorkflowDomainEvent),typeof(WorkflowActionReceipt)}){var e=b.Entity(type);e.HasBaseType((Type?)null);e.ToTable(type==typeof(WorkflowDomainEvent)?"WorkflowOutbox":"WorkflowActionReceipts");e.HasKey("Id");e.HasAlternateKey("TenantId","Id");e.Property<Guid>("Version").IsConcurrencyToken();}
  b.Entity<WorkflowDomainEvent>().Property(x=>x.EntityKey).HasMaxLength(80);b.Entity<WorkflowDomainEvent>().Property(x=>x.Kind).HasMaxLength(80);
  b.Entity<WorkflowDomainEvent>().Property(x=>x.CurrentJson).HasMaxLength(262144);b.Entity<WorkflowDomainEvent>().Property(x=>x.PreviousJson).HasMaxLength(262144);b.Entity<WorkflowDomainEvent>().Property(x=>x.AncestryJson).HasMaxLength(4000);b.Entity<WorkflowDomainEvent>().HasIndex(x=>new{x.DeliveredAtUtc,x.EntityKey,x.CreatedAtUtc});
  b.Entity<WorkflowActionReceipt>().Property(x=>x.ActionKey).HasMaxLength(160);b.Entity<WorkflowActionReceipt>().Property(x=>x.ResultJson).HasMaxLength(16000);
 }
}
public sealed class WorkflowCaptureInterceptor(IEnumerable<WorkflowEntityRegistration> registrations,IRequestIdentity who,WorkflowExecutionContext execution):SaveChangesInterceptor
{
 private void Capture(DbContext? db)
 {
  if(db==null)return;var actor=(db as IWorkflowWriteIdentity)?.WorkflowIdentity??who;db.ChangeTracker.DetectChanges();
  foreach(var receipt in db.ChangeTracker.Entries<WorkflowActionReceipt>().Where(x=>x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))if(receipt.Entity.TenantId!=actor.TenantId||receipt.State!=EntityState.Added)throw new TenantBoundaryException();
  foreach(var tracked in db.ChangeTracker.Entries<WorkflowDomainEvent>().Where(x=>x.State==EntityState.Added))if(tracked.Entity.TenantId!=actor.TenantId||tracked.Entity.ActorId!=actor.UserId)throw new TenantBoundaryException();
  foreach(var entry in db.ChangeTracker.Entries().Where(x=>x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray())
  {
   var candidates=registrations.Where(r=>r.RecordType==entry.Metadata.ClrType).ToArray();if(candidates.Length==0)continue;
   if(actor.TenantId==null||actor.UserId==null||!Equals(entry.Property("TenantId").CurrentValue,actor.TenantId))throw new TenantBoundaryException();
   foreach(var r in candidates)
   {
    if(r.DiscriminatorProperty!=null&&!Equals(entry.Property(r.DiscriminatorProperty).CurrentValue,r.DiscriminatorValue))continue;
    var current=r.Snapshot(entry.CurrentValues);var previous=entry.State==EntityState.Added?new Dictionary<string,string?>():r.Snapshot(entry.OriginalValues);
    if(entry.State==EntityState.Modified&&current.All(k=>previous.GetValueOrDefault(k.Key)==k.Value))continue;
    var id=(Guid)entry.Property("Id").CurrentValue!;
    WorkflowAttributes.Create(db,r)?.Snapshot(id,current,previous,entry.State==EntityState.Added);
    if(db.ChangeTracker.Entries<WorkflowDomainEvent>().Any(e=>e.State==EntityState.Added&&e.Entity.EntityKey==r.Key&&e.Entity.RecordId==id))continue;
    var archived=current.GetValueOrDefault("isArchived")=="true"&&previous.GetValueOrDefault("isArchived")!="true";
    var context=execution.Current;
    db.Add(new WorkflowDomainEvent{TenantId=actor.TenantId.Value,ActorId=actor.UserId.Value,EntityKey=r.Key,RecordId=id,Kind=entry.State==EntityState.Added?"record.created":entry.State==EntityState.Deleted||archived?"record.deleted":"record.updated",CurrentJson=JsonSerializer.Serialize(current),PreviousJson=JsonSerializer.Serialize(previous),CausationRunId=context?.RunId,RootRunId=context?.RootRunId,AncestryJson=JsonSerializer.Serialize(context?.Ancestry??[])});
   }
  }
  var attributeIds=db.ChangeTracker.Entries().Where(e=>e.Entity is IAutomationValue && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
   .Select(e=>((IAutomationValue)e.Entity).RecordId).Distinct().ToArray();
  foreach(var id in attributeIds)
  foreach(var r in registrations.Where(r=>db.Model.FindEntityType(r.RecordType)!=null))
  {
   var record=db.Find(r.RecordType,id);if(record==null)continue;
   var entry=db.Entry(record);
   if(!Equals(entry.Property("TenantId").CurrentValue,actor.TenantId))throw new TenantBoundaryException();
   if(r.DiscriminatorProperty!=null&&!Equals(entry.Property(r.DiscriminatorProperty).CurrentValue,r.DiscriminatorValue))continue;
   var current=r.Snapshot(entry.CurrentValues);var previous=r.Snapshot(entry.OriginalValues);
   var attributes=WorkflowAttributes.Create(db,r);if(attributes==null||attributes.Fields.Length==0)continue;
   attributes.Snapshot(id,current,previous,false);
   var pending=db.ChangeTracker.Entries<WorkflowDomainEvent>().FirstOrDefault(e=>e.Entity.EntityKey==r.Key&&e.Entity.RecordId==id&&e.Entity.DeliveredAtUtc==null);
   if(pending!=null)
   {
    // A create/update and its attribute write can use two SaveChanges calls in one request transaction.
    // Keep one event with the complete final snapshot and the original previous values.
    var original=JsonSerializer.Deserialize<Dictionary<string,string?>>(pending.Entity.PreviousJson)??[];
    if(pending.Entity.Kind!="record.created")foreach(var field in attributes.Fields)original.TryAdd(field.Key,previous.GetValueOrDefault(field.Key));
    pending.Entity.CurrentJson=JsonSerializer.Serialize(current);pending.Entity.PreviousJson=JsonSerializer.Serialize(original);continue;
   }
   if(current.All(pair=>previous.GetValueOrDefault(pair.Key)==pair.Value))continue;
   if(actor.TenantId==null||actor.UserId==null)throw new TenantBoundaryException();
   var context=execution.Current;
   db.Add(new WorkflowDomainEvent{TenantId=actor.TenantId.Value,ActorId=actor.UserId.Value,EntityKey=r.Key,RecordId=id,Kind="record.updated",CurrentJson=JsonSerializer.Serialize(current),PreviousJson=JsonSerializer.Serialize(previous),CausationRunId=context?.RunId,RootRunId=context?.RootRunId,AncestryJson=JsonSerializer.Serialize(context?.Ancestry??[])});
  }
 }
 public override InterceptionResult<int> SavingChanges(DbContextEventData eventData,InterceptionResult<int> result){Capture(eventData.Context);return result;}
 public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,InterceptionResult<int> result,CancellationToken ct=default){Capture(eventData.Context);return ValueTask.FromResult(result);}
}
