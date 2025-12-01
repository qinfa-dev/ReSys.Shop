namespace ReSys.Core.Feature.Common.Notification.Constants;

public static partial class NotificationConstants
{
    public enum Parameter
    {
        // System-related parameters
        SystemName,                // Name of the fashion e-shop or application (e.g., "TrendyThreads")
        SupportEmail,              // Support email address for customer service (e.g., "support@trendythreads.com")
        SupportPhone,              // Support phone number for customer service (e.g., "+1-800-555-1234")
        CustomerSupportLink,       // URL to the customer support page (e.g., "https://trendythreads.com/support")

        // User-related parameters
        UserName,                  // Username of the user (e.g., "fashionista123")
        UserEmail,                 // Email address of the user (e.g., "user@domain.com")
        UserFullName,              // Full name of the user (e.g., "Jane Doe")
        UserFirstName,             // First name of the user (e.g., "Jane")
        UserLastName,              // Last name of the user (e.g., "Doe")
        UserProfileUrl,            // URL to the user's profile page (e.g., "https://trendythreads.com/profile")
        UserFavoriteCategory,      // User's favorite fashion category (e.g., "Dresses", "Streetwear")
        OtpCode,                   // One-time password for two-factor authentication (e.g., "123456")

        // Order-related parameters
        OrderId,                   // Unique identifier for the order (e.g., "ORD123456")
        OrderDate,                 // Date the order was placed (e.g., "2025-05-31")
        OrderTotal,                // Total cost of the order (e.g., "$150.00")
        OrderStatus,               // Status of the order (e.g., "Processing", "Shipped")
        OrderTrackingNumber,       // Tracking number for the order shipment (e.g., "1Z9999W999999999")
        OrderTrackingUrl,          // URL to track the shipment (e.g., "https://carrier.com/track/1Z9999W999999999")
        OrderItems,                // List of items in the order (e.g., "Red Dress, Black Sneakers")

        // Payment-related parameters
        PaymentStatus,             // Status of the payment (e.g., "Successful", "Failed")
        PaymentAmount,             // Amount paid for the order (e.g., "$150.00")
        PaymentMethod,             // Payment method used (e.g., "Credit Card", "PayPal")

        // Link-related parameters
        ActiveUrl,                 // URL for activating the user's account (e.g., "https://trendythreads.com/activate?token=xyz")
        ResetPasswordUrl,          // URL for resetting the user's password (e.g., "https://trendythreads.com/reset?token=xyz")
        UnsubscribeUrl,            // URL for unsubscribing from marketing emails (e.g., "https://trendythreads.com/unsubscribe")
        SurveyUrl,                 // URL for a marketing survey (e.g., "https://trendythreads.com/survey")
        SiteUrl,                   // Base URL of the e-shop (e.g., "https://trendythreads.com")

        // Time-related parameters
        CreatedDateTimeOffset,           // Date and time when the user or order was created (e.g., "2025-05-31 09:46:00")
        ExpiryDateTimeOffset,            // Expiry date and time for a promotion or offer (e.g., "2025-06-07 23:59:59")
        DeliveryDate,              // Expected or actual delivery date for the order (e.g., "2025-06-05")

        // Promotional parameters
        PromoCode,                 // Promotional code applied to the order (e.g., "SUMMER25")
        PromoDiscount,             // Discount amount applied via promo code (e.g., "25% off")
        PromoUrl,                  // URL for the promotional offer or sale page (e.g., "https://trendythreads.com/summer-sale")

        // Fashion-specific parameters
        CollectionName,            // Name of a new or featured collection (e.g., "Summer Chic Collection")
        CollectionUrl,             // URL to a specific collection (e.g., "https://trendythreads.com/collections/summer-chic")
        RecentProductView,         // Name or link of a recently viewed product (e.g., "Floral Midi Dress")
        RecommendedProductUrl,     // URL to a recommended product based on user behavior (e.g., "https://trendythreads.com/products/floral-dress")
        LoyaltyPoints,             // User's current loyalty points balance (e.g., "150 points")
        LoyaltyRewardUrl           // URL to redeem loyalty rewards (e.g., "https://trendythreads.com/rewards")
    }

    public static readonly Dictionary<Parameter, Definition<Parameter>> Parameters = new()
    {
        #region System-related parameters
        [key: Parameter.SystemName] = new Definition<Parameter>
        {
            Value = Parameter.SystemName,
            Presentation = "System Name",
            Description = "The name of the fashion e-shop or application, used for branding in notifications.",
            SampleData = "TrendyThreads"
        },
        [key: Parameter.SupportEmail] = new Definition<Parameter>
        {
            Value = Parameter.SupportEmail,
            Presentation = "Support Email",
            Description = "The email address for customer service, provided for user support inquiries.",
            SampleData = "support@trendythreads.com"
        },
        [key: Parameter.SupportPhone] = new Definition<Parameter>
        {
            Value = Parameter.SupportPhone,
            Presentation = "Support Phone",
            Description = "The phone number for customer service, used for urgent or direct support contact.",
            SampleData = "+1-800-555-1234"
        },
        [key: Parameter.CustomerSupportLink] = new Definition<Parameter>
        {
            Value = Parameter.CustomerSupportLink,
            Presentation = "Customer Support Link",
            Description = "URL to the customer support page, directing users to help resources.",
            SampleData = "https://trendythreads.com/support"
        },
        #endregion

        #region User-related parameters
        [key: Parameter.UserName] = new Definition<Parameter>
        {
            Value = Parameter.UserName,
            Presentation = "User Name",
            Description = "The username of the user, used for login or identification purposes.",
            SampleData = "fashionista123"
        },
        [key: Parameter.UserEmail] = new Definition<Parameter>
        {
            Value = Parameter.UserEmail,
            Presentation = "User Email",
            Description = "The email address of the user, used for communication and notifications.",
            SampleData = "jane.doe@domain.com"
        },
        [key: Parameter.UserFullName] = new Definition<Parameter>
        {
            Value = Parameter.UserFullName,
            Presentation = "User Full Name",
            Description = "The full name of the user, used for personalized greetings in notifications.",
            SampleData = "Jane Doe"
        },
        [key: Parameter.UserFirstName] = new Definition<Parameter>
        {
            Value = Parameter.UserFirstName,
            Presentation = "User First Name",
            Description = "The first name of the user, used for a friendly and personal tone.",
            SampleData = "Jane"
        },
        [key: Parameter.UserLastName] = new Definition<Parameter>
        {
            Value = Parameter.UserLastName,
            Presentation = "User Last Name",
            Description = "The last name of the user, used in formal or full-name contexts.",
            SampleData = "Doe"
        },
        [key: Parameter.UserProfileUrl] = new Definition<Parameter>
        {
            Value = Parameter.UserProfileUrl,
            Presentation = "User Profile URL",
            Description = "URL to the user's profile page, allowing quick access to account settings.",
            SampleData = "https://trendythreads.com/profile"
        },
        [key: Parameter.UserFavoriteCategory] = new Definition<Parameter>
        {
            Value = Parameter.UserFavoriteCategory,
            Presentation = "User Favorite Category",
            Description = "The user's preferred fashion category, used for personalized recommendations.",
            SampleData = "Dresses"
        },
        [key: Parameter.OtpCode] = new Definition<Parameter>
        {
            Value = Parameter.OtpCode,
            Presentation = "OTP Code",
            Description = "One-time password for two-factor authentication, used for secure login or actions.",
            SampleData = "123456"
        },
        #endregion

        #region Order-related parameters
        [key: Parameter.OrderId] = new Definition<Parameter>
        {
            Value = Parameter.OrderId,
            Presentation = "Order ID",
            Description = "Unique identifier for the order, used to reference specific purchases.",
            SampleData = "ORD123456"
        },
        [key: Parameter.OrderDate] = new Definition<Parameter>
        {
            Value = Parameter.OrderDate,
            Presentation = "Order Date",
            Description = "The date the order was placed, providing context for the purchase timeline.",
            SampleData = "2025-05-31"
        },
        [key: Parameter.OrderTotal] = new Definition<Parameter>
        {
            Value = Parameter.OrderTotal,
            Presentation = "Order Total",
            Description = "The total cost of the order, including all items and fees.",
            SampleData = "$150.00"
        },
        [key: Parameter.OrderStatus] = new Definition<Parameter>
        {
            Value = Parameter.OrderStatus,
            Presentation = "Order Status",
            Description = "The current status of the order, such as Processing, Shipped, or Delivered.",
            SampleData = "Shipped"
        },
        [key: Parameter.OrderTrackingNumber] = new Definition<Parameter>
        {
            Value = Parameter.OrderTrackingNumber,
            Presentation = "Order Tracking Number",
            Description = "The tracking number for the order shipment, used for tracking delivery.",
            SampleData = "1Z9999W999999999"
        },
        [key: Parameter.OrderTrackingUrl] = new Definition<Parameter>
        {
            Value = Parameter.OrderTrackingUrl,
            Presentation = "Order Tracking URL",
            Description = "URL to track the shipment, linking to the carrier�s tracking page.",
            SampleData = "https://carrier.com/track/1Z9999W999999999"
        },
        [key: Parameter.OrderItems] = new Definition<Parameter>
        {
            Value = Parameter.OrderItems,
            Presentation = "Order Items",
            Description = "List of items in the order, providing details of purchased products.",
            SampleData = "Red Dress, Black Sneakers"
        },
        #endregion

        #region Payment-related parameters
        [key: Parameter.PaymentStatus] = new Definition<Parameter>
        {
            Value = Parameter.PaymentStatus,
            Presentation = "Payment Status",
            Description = "The status of the payment, such as Successful or Failed.",
            SampleData = "Successful"
        },
        [key: Parameter.PaymentAmount] = new Definition<Parameter>
        {
            Value = Parameter.PaymentAmount,
            Presentation = "Payment Amount",
            Description = "The amount paid for the order, reflecting the transaction value.",
            SampleData = "$150.00"
        },
        [key: Parameter.PaymentMethod] = new Definition<Parameter>
        {
            Value = Parameter.PaymentMethod,
            Presentation = "Payment Method",
            Description = "The payment method used for the order, such as Credit Card or PayPal.",
            SampleData = "Credit Card"
        },
        #endregion

        #region Link-related parameters
        [key: Parameter.ActiveUrl] = new Definition<Parameter>
        {
            Value = Parameter.ActiveUrl,
            Presentation = "Activation URL",
            Description = "URL for activating the user�s account, used during registration.",
            SampleData = "https://trendythreads.com/activate?token=xyz"
        },
        [key: Parameter.ResetPasswordUrl] = new Definition<Parameter>
        {
            Value = Parameter.ResetPasswordUrl,
            Presentation = "Reset Password URL",
            Description = "URL for resetting the user�s password, used in password recovery.",
            SampleData = "https://trendythreads.com/reset?token=xyz"
        },
        [key: Parameter.UnsubscribeUrl] = new Definition<Parameter>
        {
            Value = Parameter.UnsubscribeUrl,
            Presentation = "Unsubscribe URL",
            Description = "URL for unsubscribing from marketing emails, ensuring compliance with regulations.",
            SampleData = "https://trendythreads.com/unsubscribe"
        },
        [key: Parameter.SurveyUrl] = new Definition<Parameter>
        {
            Value = Parameter.SurveyUrl,
            Presentation = "Survey URL",
            Description = "URL for a marketing survey, used to collect user feedback.",
            SampleData = "https://trendythreads.com/survey"
        },
        [key: Parameter.SiteUrl] = new Definition<Parameter>
        {
            Value = Parameter.SiteUrl,
            Presentation = "Site URL",
            Description = "Base URL of the e-shop, used for general navigation links.",
            SampleData = "https://trendythreads.com"
        },
        #endregion

        #region Time-related parameters
        [key: Parameter.CreatedDateTimeOffset] = new Definition<Parameter>
        {
            Value = Parameter.CreatedDateTimeOffset,
            Presentation = "Created DateTimeOffset",
            Description = "Date and time when the user or order was created, for record-keeping.",
            SampleData = "2025-05-31 09:46:00"
        },
        [key: Parameter.ExpiryDateTimeOffset] = new Definition<Parameter>
        {
            Value = Parameter.ExpiryDateTimeOffset,
            Presentation = "Expiry DateTimeOffset",
            Description = "Expiry date and time for a promotion or offer, creating urgency.",
            SampleData = "2025-06-07 23:59:59"
        },
        [key: Parameter.DeliveryDate] = new Definition<Parameter>
        {
            Value = Parameter.DeliveryDate,
            Presentation = "Delivery Date",
            Description = "Expected or actual delivery date for the order, informing users of timelines.",
            SampleData = "2025-06-05"
        },
        #endregion

        #region Promotional parameters
        [key: Parameter.PromoCode] = new Definition<Parameter>
        {
            Value = Parameter.PromoCode,
            Presentation = "Promo Code",
            Description = "Promotional code applied to the order, offering discounts or incentives.",
            SampleData = "SUMMER25"
        },
        [key: Parameter.PromoDiscount] = new Definition<Parameter>
        {
            Value = Parameter.PromoDiscount,
            Presentation = "Promo Discount",
            Description = "Discount amount applied via a promo code, such as a percentage or fixed amount.",
            SampleData = "25% off"
        },
        [key: Parameter.PromoUrl] = new Definition<Parameter>
        {
            Value = Parameter.PromoUrl,
            Presentation = "Promo URL",
            Description = "URL for the promotional offer or sale page, directing users to specific campaigns.",
            SampleData = "https://trendythreads.com/summer-sale"
        },
        #endregion

        #region Fashion-specific parameters
        [key: Parameter.CollectionName] = new Definition<Parameter>
        {
            Value = Parameter.CollectionName,
            Presentation = "Collection Name",
            Description = "Name of a new or featured fashion collection, used in promotional notifications.",
            SampleData = "Summer Chic Collection"
        },
        [key: Parameter.CollectionUrl] = new Definition<Parameter>
        {
            Value = Parameter.CollectionUrl,
            Presentation = "Collection URL",
            Description = "URL to a specific fashion collection, linking to curated product pages.",
            SampleData = "https://trendythreads.com/collections/summer-chic"
        },
        [key: Parameter.RecentProductView] = new Definition<Parameter>
        {
            Value = Parameter.RecentProductView,
            Presentation = "Recent Product View",
            Description = "Name or link of a recently viewed product, used for re-engagement.",
            SampleData = "Floral Midi Dress"
        },
        [key: Parameter.RecommendedProductUrl] = new Definition<Parameter>
        {
            Value = Parameter.RecommendedProductUrl,
            Presentation = "Recommended Product URL",
            Description = "URL to a recommended product based on user behavior, enhancing personalization.",
            SampleData = "https://trendythreads.com/products/floral-dress"
        },
        [key: Parameter.LoyaltyPoints] = new Definition<Parameter>
        {
            Value = Parameter.LoyaltyPoints,
            Presentation = "Loyalty Points",
            Description = "User�s current loyalty points balance, used to encourage reward redemption.",
            SampleData = "150 points"
        },
        [key: Parameter.LoyaltyRewardUrl] = new Definition<Parameter>
        {
            Value = Parameter.LoyaltyRewardUrl,
            Presentation = "Loyalty Reward URL",
            Description = "URL to redeem loyalty rewards, linking to the rewards program page.",
            SampleData = "https://trendythreads.com/rewards"
        }
        #endregion
    };
}
