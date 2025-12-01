namespace ReSys.Core.Feature.Common.Notification.Constants;

public static partial class NotificationConstants
{
    public enum SendMethod
    {
        None = 0,
        Email = 1,
        SMS = 2,
        PushNotification = 3, // For mobile app notifications (e.g., new arrivals, order updates)
        WhatsApp = 4         // Popular for fashion brands in certain regions
    }

    public static readonly Dictionary<SendMethod, Definition<SendMethod>> Definitions = new()
    {
        [key: SendMethod.None] = new Definition<SendMethod>
        {
            Value = SendMethod.None,
            Presentation = "None",
            Description = "No notification will be sent. Used for draft or inactive templates.",
            SampleData = string.Empty
        },
        [key: SendMethod.Email] = new Definition<SendMethod>
        {
            Value = SendMethod.Email,
            Presentation = "Email",
            Description = "Sends notifications via email. Ideal for order confirmations, promotions, and account updates.",
            SampleData = "Subject: Your Order #ORD123456 Has Shipped!"
        },
        [key: SendMethod.SMS] = new Definition<SendMethod>
        {
            Value = SendMethod.SMS,
            Presentation = "SMS",
            Description = "Sends notifications via text message. Best for short, time-sensitive alerts like delivery updates or OTP codes.",
            SampleData = "Your OTP code is 123456."
        },
        [key: SendMethod.PushNotification] = new Definition<SendMethod>
        {
            Value = SendMethod.PushNotification,
            Presentation = "Push Notification",
            Description = "Sends push notifications through a mobile app. Commonly used for promotions, flash sales, and real-time order status updates.",
            SampleData = "Flash Sale! Get 20% off for the next 2 hours."
        },
        [key: SendMethod.WhatsApp] = new Definition<SendMethod>
        {
            Value = SendMethod.WhatsApp,
            Presentation = "WhatsApp",
            Description = "Sends notifications via WhatsApp. Popular in some regions for direct customer engagement and order support.",
            SampleData = "Hi Jane, your order #ORD123456 has been delivered. Thank you for shopping with us!"
        }
    };
}
