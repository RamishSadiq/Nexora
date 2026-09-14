using Nexora.BuildingBlocks.Domain;
namespace Nexora.Modules.Engagement.Domain;
public sealed class Community:TenantEntity {public string Kind{get;set;}="group";public string Name{get;set;}="";public string Purpose{get;set;}="";}
public sealed class CommunityMember:TenantEntity {public Guid CommunityId{get;set;}public Guid ContactId{get;set;}public string ContactName{get;set;}="";public string Role{get;set;}="member";}
public sealed class CommunityMeeting:TenantEntity {public Guid CommunityId{get;set;}public string Subject{get;set;}="";public string Agenda{get;set;}="";public DateTime StartsAtUtc{get;set;}}
public sealed class Campaign:TenantEntity {public Guid CommunityId{get;set;}public string Name{get;set;}="";public string Subject{get;set;}="";public string Status{get;set;}="draft";}
public sealed class CampaignRecipient:TenantEntity,IAppendOnly {public Guid CampaignId{get;set;}public Guid ContactId{get;set;}public string Email{get;set;}="";}
public sealed class Fund:TenantEntity {public string Name{get;set;}="";public string Currency{get;set;}="GBP";public decimal Target{get;set;}}
public sealed class Contribution:TenantEntity,IAppendOnly {public Guid FundId{get;set;}public Guid ContactId{get;set;}public string ContactName{get;set;}="";public string Kind{get;set;}="donation";public decimal Amount{get;set;}public string Reference{get;set;}="";}
public sealed class EngagementHistory:TenantEntity,IAppendOnly {public Guid SubjectId{get;set;}public Guid ActorUserId{get;set;}public string Action{get;set;}="";public string Reason{get;set;}="";public string CorrelationId{get;set;}="";}
