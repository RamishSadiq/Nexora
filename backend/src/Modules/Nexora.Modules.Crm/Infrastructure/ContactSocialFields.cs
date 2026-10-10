using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Nexora.Modules.Crm.Contracts;
using Nexora.Modules.Crm.Domain;

namespace Nexora.Modules.Crm.Infrastructure;

internal static class ContactSocialFields
{
    internal static async Task<ContactSocialMediaDetails?> ReadAsync(CrmDbContext db, Guid id, CancellationToken ct)
    {
        var profiles = await db.Set<ContactSocialProfile>().Where(x => x.RecordId == id).ToDictionaryAsync(x => x.Channel, x => x.Identifier, ct);
        var metrics = await db.Set<ContactProfile>().SingleOrDefaultAsync(x => x.RecordId == id, ct);
        if (profiles.Count == 0 && metrics == null) return null;
        return new(profiles.GetValueOrDefault("facebook"), profiles.GetValueOrDefault("x"), profiles.GetValueOrDefault("linkedin"), profiles.GetValueOrDefault("youtube"), profiles.GetValueOrDefault("online-communities"), profiles.GetValueOrDefault("google"),
            metrics?.NumberOfFollowers, metrics?.NumberOfFollowing, metrics?.NumberOfTweetsSent, metrics?.NumberOfMentions, metrics?.NumberOfTweetsWithTags, metrics?.SocialMetricsObservedOn, metrics?.SocialMetricsSource);
    }

    private static string? Text(string? value, int maximum)
    {
        value = value?.Trim();
        if (value?.Length > maximum || value?.Any(char.IsControl) == true)
            throw new ValidationException($"Social details must be at most {maximum} characters without control characters.");
        return string.IsNullOrEmpty(value) ? null : value;
    }

    internal static async Task ApplyAsync(CrmDbContext db, CrmRecord record, ContactSocialMediaDetails details, CancellationToken ct)
    {
        foreach (var count in new[] { details.NumberOfFollowers, details.NumberOfFollowing, details.NumberOfTweetsSent, details.NumberOfMentions, details.NumberOfTweetsWithTags })
            if (count < 0 || count > 9007199254740991L || (count.HasValue && decimal.Truncate(count.Value) != count.Value)) throw new ValidationException("Social counts must be whole numbers from 0 to 9007199254740991, or unset.");
        if (details.MetricsObservedOn == DateOnly.MinValue || details.MetricsObservedOn > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ValidationException("Enter a valid metrics observation date on or before today.");
        var source = Text(details.MetricsSource, 160);
        var identifiers = new Dictionary<string, string?> {
            ["facebook"] = Text(details.FacebookId, 320), ["x"] = Text(details.XId, 320), ["linkedin"] = Text(details.LinkedInId, 320),
            ["youtube"] = Text(details.YouTubeId, 320), ["online-communities"] = Text(details.OnlineCommunities, 320), ["google"] = Text(details.GoogleId, 320) };
        var profiles = await db.Set<ContactSocialProfile>().Where(x => x.RecordId == record.Id).ToDictionaryAsync(x => x.Channel, ct);
        foreach (var (channel, identifier) in identifiers)
        {
            profiles.TryGetValue(channel, out var row);
            if (identifier == null) { if (row != null) db.Remove(row); continue; }
            if (row == null) { row = new ContactSocialProfile { TenantId = record.TenantId, RecordId = record.Id, Channel = channel }; db.Add(row); }
            row.Identifier = identifier;
        }
        var metrics = db.Set<ContactProfile>().Local.SingleOrDefault(x => x.RecordId == record.Id)
            ?? await db.Set<ContactProfile>().SingleOrDefaultAsync(x => x.RecordId == record.Id, ct);
        if (metrics == null) { metrics = new ContactProfile { TenantId = record.TenantId, RecordId = record.Id }; db.Add(metrics); }
        static long? Whole(decimal? value) => value.HasValue ? (long)value.Value : null;
        metrics.NumberOfFollowers = Whole(details.NumberOfFollowers); metrics.NumberOfFollowing = Whole(details.NumberOfFollowing);
        metrics.NumberOfTweetsSent = Whole(details.NumberOfTweetsSent); metrics.NumberOfMentions = Whole(details.NumberOfMentions); metrics.NumberOfTweetsWithTags = Whole(details.NumberOfTweetsWithTags);
        metrics.SocialMetricsObservedOn = details.MetricsObservedOn; metrics.SocialMetricsSource = source;
    }
}
