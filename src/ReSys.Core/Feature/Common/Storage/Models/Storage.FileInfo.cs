namespace ReSys.Core.Feature.Common.Storage.Models;
public sealed class StorageFileModel
{
    public required string Path { get; init; }
    public long? Size { get; init; }
    public DateTimeOffset? LastModifiedUtc { get; init; }
    public string? Url { get; init; }
}
