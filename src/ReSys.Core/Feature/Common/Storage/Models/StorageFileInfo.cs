namespace ReSys.Core.Feature.Common.Storage.Models;

public sealed record StorageFileInfo
{
    public required string Url { get; init; }
    public required string ContentType { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public long Size { get; init; }
    public string? DimensionsUnit { get; set; }
    public DateTimeOffset LastModified { get; init; }
}