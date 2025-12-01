namespace ReSys.Core.Feature.Common.Notification.Constants;

public static partial class NotificationConstants
{
    public record Definition<TEnumerate> where TEnumerate : Enum
    {
        public required TEnumerate Value { get; set; }
        public string Presentation { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? SampleData { get; set; } = null!;
    }

}
