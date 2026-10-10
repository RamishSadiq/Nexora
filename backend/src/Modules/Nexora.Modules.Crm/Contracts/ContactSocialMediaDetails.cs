namespace Nexora.Modules.Crm.Contracts;

public sealed record ContactSocialMediaDetails(
    string? FacebookId = null, string? XId = null, string? LinkedInId = null,
    string? YouTubeId = null, string? OnlineCommunities = null, string? GoogleId = null,
    decimal? NumberOfFollowers = null, decimal? NumberOfFollowing = null, decimal? NumberOfTweetsSent = null,
    decimal? NumberOfMentions = null, decimal? NumberOfTweetsWithTags = null,
    DateOnly? MetricsObservedOn = null, string? MetricsSource = null);
