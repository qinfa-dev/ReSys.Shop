# Notification Constants

This directory centralizes all constant definitions related to the notification system. It defines the various parameters, priorities, send methods, template formats, and use cases for notifications, ensuring consistency and easy management of notification-related configurations.

---

## Contents

### 1. Notification Parameters (`Notification.Parameters.cs`)

This file defines an enumeration of all dynamic parameters that can be injected into notification templates. It also provides detailed descriptions and sample data for each parameter, aiding in template creation and understanding.

-   **`NotificationParameter` Enum**: Lists all available parameters (e.g., `SystemName`, `UserName`, `OrderId`, `ActiveUrl`, `PromoCode`).
-   **`ParameterDescription` Class**: Provides metadata for each parameter, including its name, description, and sample usage.

### 2. Notification Priorities (`Notification.Priorities.cs`)

This file defines the priority levels for notifications, influencing their urgency and delivery.

-   **`NotificationPriority` Enum**: Defines `Low`, `Normal`, and `High` priority levels.
-   **`PriorityDescription` Class**: Offers descriptions and sample messages for each priority level.

### 3. Notification Send Methods (`Notification.SendMethods.cs`)

This file specifies the various channels through which notifications can be sent.

-   **`NotificationSendMethod` Enum**: Includes options like `Email`, `SMS`, `PushNotification`, and `WhatsApp`.
-   **`SendMethodDescription` Class**: Provides details and examples for each send method.

### 4. Notification Template Formats (`Notification.TemplateFormats.cs`)

This file defines the supported formats for notification templates.

-   **`NotificationFormat` Enum**: Specifies `Default` (plain text) and `Html` formats.
-   **`FormatDescription` Class**: Offers descriptions and sample content for each format.

### 5. Notification Use Cases (`Notification.UseCases.cs`)

This file defines an enumeration of all distinct notification scenarios and provides default template configurations for each.

-   **`NotificationUseCase` Enum**: Categorizes notifications into system, user, payment, marketing, and fashion-specific types (e.g., `SystemActiveEmail`, `UserWelcomeEmail`, `PaymentSuccessEmail`, `NewCollectionLaunch`).
-   **`TemplateDescription` Class**: For each use case, it specifies:
    -   The default `SendMethodType` and `TemplateFormatType`.
    -   A list of `ParamValues` (required parameters for the template).
    -   Default `TemplateContent` (plain text) and `HtmlTemplateContent`.
    -   A descriptive `Name` and `Description` for the use case.

---

## Purpose

The constants defined here are crucial for:

-   **Standardization**: Ensuring a consistent approach to notifications across the application.
-   **Configuration**: Providing a clear structure for defining and managing notification templates and their associated data.
-   **Extensibility**: Allowing easy addition of new notification types, parameters, or send methods.
-   **Developer Experience**: Offering clear guidance on what data is available and how notifications should be structured.
