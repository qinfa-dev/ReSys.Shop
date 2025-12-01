# Notification Models

This directory defines the data structures and associated validation logic for various types of notifications within the application. It provides a generic `NotificationData` model and specialized models for email and SMS, ensuring a consistent and extensible approach to handling notification content and metadata.

---

## Models

### 1. Base Notification Data (`NotificationData.cs`, `NotificationData.Errors.cs`)

This is the foundational model for all notifications, encapsulating common properties regardless of the delivery method.

-   **`NotificationData.cs`**: Defines core properties such as `UseCase`, `SendMethodType`, `TemplateFormatType`, `Receivers`, `Title`, `Content`, `HtmlContent`, `CreatedBy`, `Priority`, `Language`, and dynamic `Values` for template parameter replacement.
-   **`NotificationData.Errors.cs`**: Provides a set of standardized `ErrorOr.Error` definitions for validating the generic `NotificationData` instance, covering missing fields, invalid formats, and content length constraints.

### 2. Email Notification Data (`Emails/EmailNotificationData.cs`, `Emails/EmailNotificationData.Errors.cs`)

This model extends the base notification data with properties specific to email notifications.

-   **`EmailNotificationData.cs`**: Includes email-specific fields like `Cc`, `Bcc`, and `Attachments`. It also contains a `Validate()` method to ensure the email notification data is correctly formed.
-   **`EmailNotificationData.Errors.cs`**: Defines specific `ErrorOr.Error` types for email notification validation, such as missing title or content, or an invalid send method.

### 3. SMS Notification Data (`Sms/SmsNotificationData.cs`, `Sms/SmsNotificationData.Errors.cs`)

This model extends the base notification data with properties specific to SMS notifications.

-   **`SmsNotificationData.cs`**: Includes SMS-specific fields like `SenderNumber`, `IsUnicode`, and `TrackingId`. It also contains a `Validate()` method to ensure the SMS notification data is correctly formed.
-   **`SmsNotificationData.Errors.cs`**: Defines specific `ErrorOr.Error` types for SMS notification validation, such as missing sender number, receivers, or content, and content length constraints.

### 4. Notification Data Mapper (`NotificationData.Mapper.cs`)

This static class provides extension methods to facilitate the conversion of a generic `NotificationData` instance into its specific email or SMS counterparts.

-   **`ToSmsNotificationData(this NotificationData notificationData)`**: Maps a generic `NotificationData` to an `SmsNotificationData` instance, performing placeholder replacement in the content.
-   **`ToEmailNotificationData(this NotificationData notificationData)`**: Maps a generic `NotificationData` to an `EmailNotificationData` instance, performing placeholder replacement in the title, content, and HTML content.

---

## Purpose

These models and mappers are essential for:

-   **Type Safety**: Ensuring that notification data adheres to defined structures.
-   **Validation**: Providing built-in validation to catch common errors before attempting to send notifications.
-   **Flexibility**: Allowing the system to handle different notification types with a unified approach.
-   **Template Integration**: Facilitating the dynamic population of notification templates with personalized data.
