namespace ShotLens.Windows.Platform.Capture;

public sealed record CaptureDiagnosticManifest(
    int SchemaVersion,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<CaptureDiagnosticOutput> Outputs);

public sealed record CaptureDiagnosticOutput(
    string Id,
    string DeviceName,
    int AdapterIndex,
    int OutputIndex,
    int DesktopX,
    int DesktopY,
    int Width,
    int Height,
    uint DpiX,
    uint DpiY,
    bool IsPrimary,
    string? FileName,
    string Status,
    string? Error);
