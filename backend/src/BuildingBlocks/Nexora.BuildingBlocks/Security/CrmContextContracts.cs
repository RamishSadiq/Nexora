namespace Nexora.BuildingBlocks.Security;
public sealed record CrmContextItem(Guid Id, string Label, string Status, DateTime AtUtc, string Url);
public sealed record CrmContextSection(string Title, int Total, IReadOnlyList<CrmContextItem> Items);
public interface ICrmContextProvider
{
    string[] Permissions { get; }
    Task<Guid[]> SearchRecordIdsAsync(string term, string kind, CancellationToken ct) => Task.FromResult(Array.Empty<Guid>());
    Task<IReadOnlyList<CrmContextSection>> ReadAsync(Guid recordId, string kind, CancellationToken ct);
}
