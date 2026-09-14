namespace Nexora.BuildingBlocks.Platform;

public sealed record PlatformDescriptor(
    string Product,
    string Service,
    string Version,
    IReadOnlyCollection<string> Capabilities);
