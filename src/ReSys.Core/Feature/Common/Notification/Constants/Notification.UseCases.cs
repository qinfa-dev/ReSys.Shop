namespace ReSys.Core.Feature.Common.Notification.Constants;

public static partial class NotificationConstants
{
    public enum UseCase
    {
        None = 0, // Default value indicating no specific use case

        // System notifications
        SystemActiveEmail,               // Account activation email sent to verify a new user's email address
        SystemActivePhone,               // Phone activation email sent to verify a new user's phone number
        SystemResetPassword,             // Password reset email sent when a user requests to reset their password
        System2FaOtp,                   // Two-factor authentication OTP sent for additional login security
        SystemOrderConfirmation,        // Order confirmation email sent after a successful order placement
        SystemOrderShipped,             // Order shipped email sent with tracking information when an order is dispatched
        SystemOrderFailed,              // Order failed email sent when an order cannot be processed
        SystemAccountUpdate,            // Account update email sent when a user's account information is modified
        SystemPromotionEmail,           // General promotional email sent for non-specific marketing campaigns

        // User notifications
        UserWelcomeEmail,               // Welcome email sent to new users upon successful registration
        UserProfileUpdateEmail,         // Profile update notification sent when a user updates their profile details
        UserPasswordChangeNotification, // Password change notification sent to confirm a password update

        // Payment notifications
        PaymentSuccessEmail,            // Payment success email sent to confirm a successful transaction
        PaymentFailureEmail,            // Payment failure email sent when a payment attempt fails
        PaymentRefundNotification,      // Payment refund notification sent when a refund is processed

        // Marketing notifications
        MarketingNewsletter,            // Newsletter email sent to subscribers with company updates and promotions
        MarketingDiscountOffer,         // Discount offer email sent to promote a specific sale or promo code
        MarketingSurvey,                // Customer survey invitation email sent to collect user feedback

        // Fashion-specific notifications
        NewCollectionLaunch,            // Notification sent to announce a new fashion collection
        FlashSaleNotification,          // Time-sensitive notification sent to alert users about a flash sale
        BackInStockNotification,        // Notification sent when a previously out-of-stock item is available
        LoyaltyRewardEarned,            // Notification sent when a user earns loyalty points
        AbandonedCartReminder,          // Reminder email sent to users who left items in their cart
        WishlistItemOnSale              // Notification sent when a wishlisted item is discounted
    }

    public sealed record Template : Definition<UseCase>
    {
        public SendMethod SendMethodType { get; init; } = SendMethod.Email;
        public NotificationFormats.Enumerate TemplateFormatType { get; init; } = NotificationFormats.Enumerate.Default;
        public List<Parameter> ParamValues { get; init; } = [];
        public required string Name { get; init; }
        public string? TemplateContent { get; init; }
        public string? HtmlTemplateContent { get; init; }
    }

    public static readonly Dictionary<UseCase, Template> Templates = new()
    {
        [key: UseCase.SystemActiveEmail] = new Template
        {
            Value = UseCase.SystemActiveEmail,
            Name = "Account Activation Email",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nWelcome to {SystemName}! Your style journey starts here. Activate your account to unlock exclusive offers and shop the latest trends:\n\n{ActiveUrl}\n\nQuestions? Reach out at {SupportEmail}.\n\nHappy styling!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Welcome to {SystemName}! Your style journey starts here. Activate your account to unlock exclusive offers and shop the latest trends:</p><p><a href='{ActiveUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Activate Your Account</a></p><p>Questions? Reach out at <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p><p style='color: #777;'>Happy styling!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a> | <a href='{UnsubscribeUrl}'>Unsubscribe</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.ActiveUrl,
                Parameter.SystemName,
                Parameter.SupportEmail,
                Parameter.UnsubscribeUrl
            ],
            Description = "Sent to users upon registration, inviting them to activate their account with a stylish, branded welcome message.",
        },

        [key: UseCase.SystemActivePhone] = new Template
        {
            Value = UseCase.SystemActivePhone,
            Name = "Account Activation SMS",
            TemplateFormatType = NotificationFormats.Enumerate.Default,
            SendMethodType = SendMethod.SMS,
            TemplateContent = "Hi {UserFirstName}, activate your {SystemName} account using this code: {OtpCode}. Need help? Call {SupportPhone}.",
            HtmlTemplateContent = "Hi {UserFirstName},<br><br>Activate your {SystemName} account using this code: <b>{OtpCode}</b>.<br><br>Need help? Call <a href='tel:{SupportPhone}'>{SupportPhone}</a>.",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.OtpCode,
                Parameter.SystemName,
                Parameter.SupportPhone
            ],
            Description = "Sent via SMS to users to activate their account using a one-time code."
        },

        [key: UseCase.SystemResetPassword] = new Template
        {
            Value = UseCase.SystemResetPassword,
            Name = "Password Reset Email",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nNeed to reset your password? No worries! Click below to set a new one:\n\n{ResetPasswordUrl}\n\nIf you didn't request this, contact us at {SupportEmail}.\n\nStay fabulous!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Need to reset your password? No worries! Click below to set a new one:</p><p><a href='{ResetPasswordUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p><p>If you didn't request this, contact us at <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p><p style='color: #777;'>Stay fabulous!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a> | <a href='{UnsubscribeUrl}'>Unsubscribe</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.ResetPasswordUrl,
                Parameter.SystemName,
                Parameter.SupportEmail,
                Parameter.UnsubscribeUrl
            ],
            Description = "Sent to users when they request a password reset, providing a link to securely update their password."
        },

        [key: UseCase.System2FaOtp] = new Template
        {
            Value = UseCase.System2FaOtp,
            Name = "Two-Factor Authentication OTP",
            TemplateFormatType = NotificationFormats.Enumerate.Default,
            SendMethodType = SendMethod.SMS,
            TemplateContent = "Hi {UserFirstName}, your OTP for {SystemName} is {OtpCode}. Use it to complete your login. Didn't request this? Call {SupportPhone}.",
            HtmlTemplateContent = "Hi {UserFirstName},<br><br>Your OTP for {SystemName} is <b>{OtpCode}</b>. Use it to complete your login.<br><br>Didn't request this? Call <a href='tel:{SupportPhone}'>{SupportPhone}</a>.",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.OtpCode,
                Parameter.SystemName,
                Parameter.SupportPhone
            ],
            Description = "Sent via SMS to provide a one-time password for two-factor authentication during login or sensitive actions."
        },

        [key: UseCase.SystemOrderConfirmation] = new Template
        {
            Value = UseCase.SystemOrderConfirmation,
            Name = "Order Confirmation Email",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nYour order #{OrderId} is confirmed! Total: {OrderTotal}\nItems: {OrderItems}\n\nWe'll let you know when it ships. Shop more at {SiteUrl}!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Your order #{OrderId} is confirmed!</p><p><b>Total:</b> {OrderTotal}<br><b>Items:</b> {OrderItems}</p><p>We'll let you know when it ships.</p><p><a href='{SiteUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Shop More</a></p><p><a href='[InstagramUrl]'>Follow us on Instagram</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.OrderId,
                Parameter.OrderTotal,
                Parameter.OrderItems,
                Parameter.SystemName,
                Parameter.SiteUrl
            ],
            Description = "Sent after a successful order placement to confirm the purchase and provide order details."
        },

        [key: UseCase.SystemOrderShipped] = new Template
        {
            Value = UseCase.SystemOrderShipped,
            Name = "Order Shipped Email",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nGreat news! Your order #{OrderId} has shipped. Track it here: {OrderTrackingUrl}\n\nExpected delivery: {DeliveryDate}\n\nStay stylish!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Great news! Your order #{OrderId} has shipped.</p><p><b>Expected Delivery:</b> {DeliveryDate}</p><p><a href='{OrderTrackingUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Track Your Order</a></p><p style='color: #777;'>Stay stylish!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.OrderId,
                Parameter.OrderTrackingUrl,
                Parameter.DeliveryDate,
                Parameter.SystemName
            ],
            Description = "Sent when an order is shipped, providing tracking information and expected delivery details."
        },

        [key: UseCase.SystemOrderFailed] = new Template
        {
            Value = UseCase.SystemOrderFailed,
            Name = "Order Failed Email",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nWe're sorry, your order #{OrderId} failed ({OrderStatus}). Please try again or contact us at {SupportEmail}.\n\nWe're here to help!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>We're sorry, your order #{OrderId} failed (<b>{OrderStatus}</b>).</p><p>Please try again or contact us at <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p><p style='color: #777;'>We're here to help!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.OrderId,
                Parameter.OrderStatus,
                Parameter.SystemName,
                Parameter.SupportEmail
            ],
            Description = "Sent when an order fails to process, encouraging the user to retry or contact support."
        },

        [key: UseCase.SystemAccountUpdate] = new Template
        {
            Value = UseCase.SystemAccountUpdate,
            Name = "Account Update Email",
            TemplateFormatType = NotificationFormats.Enumerate.Default,
            TemplateContent = "Hi {UserFirstName},\n\nYour {SystemName} account has been updated. If you didn't make this change, contact us at {SupportEmail}.\n\nKeep shining!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Your {SystemName} account has been updated.</p><p>If you didn't make this change, contact us at <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p><p style='color: #777;'>Keep shining!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.SystemName,
                Parameter.SupportEmail
            ],
            Description = "Sent to confirm updates to a user's account information, with a security notice."
        },

        [key: UseCase.SystemPromotionEmail] = new Template
        {
            Value = UseCase.SystemPromotionEmail,
            Name = "System Promotion Email",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nDon't miss out on our special promotion at {SystemName}! Use code {PromoCode} for {PromoDiscount} off. Shop now: {PromoUrl}\n\nHappy shopping!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Don't miss out on our special promotion at {SystemName}!</p><p>Use code <b>{PromoCode}</b> for <b>{PromoDiscount} off</b>.</p><p><a href='{PromoUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Shop Now</a></p><p style='color: #777;'>Happy shopping!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a> | <a href='{UnsubscribeUrl}'>Unsubscribe</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.SystemName,
                Parameter.PromoCode,
                Parameter.PromoDiscount,
                Parameter.PromoUrl,
                Parameter.UnsubscribeUrl
            ],
            Description = "General promotional email sent for non-specific marketing campaigns."
        },

        [key: UseCase.UserWelcomeEmail] = new Template
        {
            Value = UseCase.UserWelcomeEmail,
            Name = "User Welcome Email",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nWelcome to {SystemName}! Explore {UserFavoriteCategory} styles and enjoy exclusive offers. Start shopping: {SiteUrl}\n\nHappy styling!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Welcome to {SystemName}! Explore {UserFavoriteCategory} styles and enjoy exclusive offers.</p><p><a href='{SiteUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Start Shopping</a></p><p style='color: #777;'>Happy styling!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a> | <a href='{UnsubscribeUrl}'>Unsubscribe</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.SystemName,
                Parameter.UserFavoriteCategory,
                Parameter.SiteUrl,
                Parameter.UnsubscribeUrl
            ],
            Description = "Sent to new users to welcome them and encourage exploration of the e-shop."
        },

        [key: UseCase.UserProfileUpdateEmail] = new Template
        {
            Value = UseCase.UserProfileUpdateEmail,
            Name = "User Profile Update Email",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nYour profile at {SystemName} has been updated. Not you? Contact us at {SupportEmail}.\n\nKeep shining!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Your profile at {SystemName} has been updated.</p><p>Not you? Contact us at <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p><p style='color: #777;'>Keep shining!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.SystemName,
                Parameter.SupportEmail
            ],
            Description = "Sent to confirm updates to a user's profile, with a security notice."
        },

        [key: UseCase.UserPasswordChangeNotification] = new Template
        {
            Value = UseCase.UserPasswordChangeNotification,
            Name = "User Password Change Notification",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nYour {SystemName} password has been changed. If this wasn't you, contact {SupportEmail} immediately.\n\nStay secure!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Your {SystemName} password has been changed.</p><p>If this wasn't you, contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a> immediately.</p><p style='color: #777;'>Stay secure!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.SystemName,
                Parameter.SupportEmail
            ],
            Description = "Sent to confirm a password change, with a security notice."
        },

        [key: UseCase.PaymentSuccessEmail] = new Template
        {
            Value = UseCase.PaymentSuccessEmail,
            Name = "Payment Success Email",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nYour payment of {PaymentAmount} for order #{OrderId} was successful. Thank you for shopping with {SystemName}!\n\nExplore more styles: {SiteUrl}",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Your payment of <b>{PaymentAmount}</b> for order #{OrderId} was successful.</p><p>Thank you for shopping with {SystemName}!</p><p><a href='{SiteUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Explore More Styles</a></p><p><a href='[InstagramUrl]'>Follow us on Instagram</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.PaymentAmount,
                Parameter.OrderId,
                Parameter.SystemName,
                Parameter.SiteUrl
            ],
            Description = "Sent to confirm a successful payment and encourage further shopping."
        },

        [key: UseCase.PaymentFailureEmail] = new Template
        {
            Value = UseCase.PaymentFailureEmail,
            Name = "Payment Failure Email",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nYour payment of {PaymentAmount} for order #{OrderId} failed ({PaymentStatus}). Please update your payment details at {SiteUrl} or contact {SupportEmail}.\n\nWe're here to help!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Your payment of <b>{PaymentAmount}</b> for order #{OrderId} failed (<b>{PaymentStatus}</b>).</p><p>Please update your payment details at <a href='{SiteUrl}'>{SiteUrl}</a> or contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p><p style='color: #777;'>We're here to help!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.PaymentAmount,
                Parameter.OrderId,
                Parameter.PaymentStatus,
                Parameter.SystemName,
                Parameter.SiteUrl,
                Parameter.SupportEmail
            ],
            Description = "Sent when a payment fails, guiding the user to update payment details."
        },

        [key: UseCase.PaymentRefundNotification] = new Template
        {
            Value = UseCase.PaymentRefundNotification,
            Name = "Payment Refund Notification",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nYour refund of {PaymentAmount} for order #{OrderId} has been processed. Questions? Contact us at {SupportEmail}.\n\nShop again at {SiteUrl}!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Your refund of <b>{PaymentAmount}</b> for order #{OrderId} has been processed.</p><p>Questions? Contact us at <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p><p><a href='{SiteUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Shop Again</a></p><p><a href='[InstagramUrl]'>Follow us on Instagram</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.PaymentAmount,
                Parameter.OrderId,
                Parameter.SystemName,
                Parameter.SupportEmail,
                Parameter.SiteUrl
            ],
            Description = "Sent to confirm a refund, providing details and encouraging further shopping."
        },

        [key: UseCase.MarketingNewsletter] = new Template
        {
            Value = UseCase.MarketingNewsletter,
            Name = "Marketing Newsletter",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nDiscover the latest trends and exclusive offers from {SystemName}! Check out {UserFavoriteCategory} styles: {SiteUrl}\n\nStay chic!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Discover the latest trends and exclusive offers from {SystemName}!</p><p>Check out {UserFavoriteCategory} styles:</p><p><a href='{SiteUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Explore Now</a></p><p style='color: #777;'>Stay chic!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a> | <a href='{UnsubscribeUrl}'>Unsubscribe</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.SystemName,
                Parameter.UserFavoriteCategory,
                Parameter.SiteUrl,
                Parameter.UnsubscribeUrl
            ],
            Description = "Sent to subscribers with the latest company news, trends, and promotions."
        },

        [key: UseCase.MarketingDiscountOffer] = new Template
        {
            Value = UseCase.MarketingDiscountOffer,
            Name = "Marketing Discount Offer",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nSteal the look with {PromoDiscount} off using code {PromoCode}! Shop {UserFavoriteCategory} styles before {ExpiryDateTimeOffset}: {PromoUrl}\n\nDon't miss out!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Steal the look with <b>{PromoDiscount} off</b> using code <b>{PromoCode}</b>!</p><p>Shop {UserFavoriteCategory} styles before {ExpiryDateTimeOffset}:</p><p><a href='{PromoUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Shop Now</a></p><p style='color: #777;'>Don't miss out!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a> | <a href='{UnsubscribeUrl}'>Unsubscribe</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.PromoCode,
                Parameter.PromoDiscount,
                Parameter.PromoUrl,
                Parameter.UserFavoriteCategory,
                Parameter.ExpiryDateTimeOffset,
                Parameter.SystemName,
                Parameter.UnsubscribeUrl
            ],
            Description = "Sent to promote a limited-time discount, tailored to the user's favorite fashion category."
        },

        [key: UseCase.MarketingSurvey] = new Template
        {
            Value = UseCase.MarketingSurvey,
            Name = "Marketing Survey Email",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nHelp us make {SystemName} better! Share your thoughts on {UserFavoriteCategory} styles: {SurveyUrl}\n\nThank you for your feedback!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Help us make {SystemName} better! Share your thoughts on {UserFavoriteCategory} styles:</p><p><a href='{SurveyUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Take Survey</a></p><p style='color: #777;'>Thank you for your feedback!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a> | <a href='{UnsubscribeUrl}'>Unsubscribe</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.SurveyUrl,
                Parameter.UserFavoriteCategory,
                Parameter.SystemName,
                Parameter.UnsubscribeUrl
            ],
            Description = "Sent to collect user feedback via a survey, tailored to their fashion preferences."
        },

        [key: UseCase.NewCollectionLaunch] = new Template
        {
            Value = UseCase.NewCollectionLaunch,
            Name = "New Collection Launch",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nOur {CollectionName} collection just dropped! Discover {UserFavoriteCategory} styles: {CollectionUrl}\n\nBe the first to slay the look!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Our <b>{CollectionName}</b> collection just dropped! Discover {UserFavoriteCategory} styles:</p><p><a href='{CollectionUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Shop the Collection</a></p><p style='color: #777;'>Be the first to slay the look!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a> | <a href='{UnsubscribeUrl}'>Unsubscribe</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.CollectionName,
                Parameter.CollectionUrl,
                Parameter.UserFavoriteCategory,
                Parameter.SystemName,
                Parameter.UnsubscribeUrl
            ],
            Description = "Sent to announce a new fashion collection, highlighting styles tailored to the user's preferences."
        },

        [key: UseCase.FlashSaleNotification] = new Template
        {
            Value = UseCase.FlashSaleNotification,
            Name = "Flash Sale Notification",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nHurry! Our {CollectionName} flash sale is on! Get up to {PromoDiscount} off until {ExpiryDateTimeOffset}. Shop now: {PromoUrl}\n\nDon't miss out!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Hurry! Our <b>{CollectionName}</b> flash sale is on! Get up to <b>{PromoDiscount} off</b> until {ExpiryDateTimeOffset}.</p><p><a href='{PromoUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Shop Now</a></p><p style='color: #777;'>Don't miss out!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a> | <a href='{UnsubscribeUrl}'>Unsubscribe</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.CollectionName,
                Parameter.PromoDiscount,
                Parameter.PromoUrl,
                Parameter.ExpiryDateTimeOffset,
                Parameter.SystemName,
                Parameter.UnsubscribeUrl
            ],
            Description = "Sent to alert users about a time-sensitive flash sale, creating urgency to drive purchases."
        },

        [key: UseCase.BackInStockNotification] = new Template
        {
            Value = UseCase.BackInStockNotification,
            Name = "Back In Stock Notification",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nGood news! {RecentProductView} is back in stock at {SystemName}. Grab it now: {RecommendedProductUrl}\n\nHurry, it won't last long!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Good news! <b>{RecentProductView}</b> is back in stock at {SystemName}.</p><p><a href='{RecommendedProductUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Grab It Now</a></p><p style='color: #777;'>Hurry, it won't last long!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a> | <a href='{UnsubscribeUrl}'>Unsubscribe</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.RecentProductView,
                Parameter.RecommendedProductUrl,
                Parameter.SystemName,
                Parameter.UnsubscribeUrl
            ],
            Description = "Sent when a previously out-of-stock item is available, encouraging immediate purchase."
        },

        [key: UseCase.LoyaltyRewardEarned] = new Template
        {
            Value = UseCase.LoyaltyRewardEarned,
            Name = "Loyalty Reward Earned",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nYou've earned {LoyaltyPoints} points at {SystemName}! Redeem them for exclusive rewards: {LoyaltyRewardUrl}\n\nKeep shopping to earn more!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>You've earned <b>{LoyaltyPoints} points</b> at {SystemName}!</p><p>Redeem them for exclusive rewards:</p><p><a href='{LoyaltyRewardUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Redeem Now</a></p><p style='color: #777;'>Keep shopping to earn more!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a> | <a href='{UnsubscribeUrl}'>Unsubscribe</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.LoyaltyPoints,
                Parameter.LoyaltyRewardUrl,
                Parameter.SystemName,
                Parameter.UnsubscribeUrl
            ],
            Description = "Sent to notify users of earned loyalty points, encouraging them to redeem rewards."
        },

        [key: UseCase.AbandonedCartReminder] = new Template
        {
            Value = UseCase.AbandonedCartReminder,
            Name = "Abandoned Cart Reminder",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nYou left some fabulous items in your {SystemName} cart! Complete your purchase: {SiteUrl}\n\nHurry, stock is limited!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>You left some fabulous items in your {SystemName} cart!</p><p><a href='{SiteUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Complete Your Purchase</a></p><p style='color: #777;'>Hurry, stock is limited!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a> | <a href='{UnsubscribeUrl}'>Unsubscribe</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.SystemName,
                Parameter.SiteUrl,
                Parameter.UnsubscribeUrl
            ],
            Description = "Sent to remind users about items left in their cart, encouraging them to complete the purchase."
        },

        [key: UseCase.WishlistItemOnSale] = new Template
        {
            Value = UseCase.WishlistItemOnSale,
            Name = "Wishlist Item On Sale",
            TemplateFormatType = NotificationFormats.Enumerate.Html,
            TemplateContent = "Hi {UserFirstName},\n\nGreat news! {RecentProductView} on your {SystemName} wishlist is now on sale with {PromoDiscount} off! Shop now: {RecommendedProductUrl}\n\nDon't wait!",
            HtmlTemplateContent = "<html><body style='font-family: Arial, sans-serif; color: #333; background-color: #f9f9f9; padding: 20px;'><img src='[BrandLogoUrl]' alt='{SystemName} Logo' style='max-width: 150px;'><h2 style='color: #d81b60;'>Hi {UserFirstName},</h2><p>Great news! <b>{RecentProductView}</b> on your {SystemName} wishlist is now on sale with <b>{PromoDiscount} off</b>!</p><p><a href='{RecommendedProductUrl}' style='background-color: #d81b60; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Shop Now</a></p><p style='color: #777;'>Don't wait!</p><p><a href='[InstagramUrl]'>Follow us on Instagram</a> | <a href='{UnsubscribeUrl}'>Unsubscribe</a></p></body></html>",
            ParamValues =
            [
                Parameter.UserFirstName,
                Parameter.RecentProductView,
                Parameter.PromoDiscount,
                Parameter.RecommendedProductUrl,
                Parameter.SystemName,
                Parameter.UnsubscribeUrl
            ],
            Description = "Sent when a wishlisted item is discounted, encouraging immediate purchase."
        }
    };
}
