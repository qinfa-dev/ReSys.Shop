# Common Notification Use Cases

This directory centralizes all components related to the application's notification system. It provides a structured and extensible way to define, build, and send various types of notifications (e.g., email, SMS) across different use cases.

---

## Modules

### 1. Builders (`Builders/`)

This subdirectory contains builder classes and extension methods that provide a fluent API for constructing `NotificationData` objects.

-   **Functionality**: Simplifies the creation and configuration of notification instances, ensuring consistency by integrating with predefined constants and templates. It allows for easy setup of use cases, parameters, receivers, content, and other notification properties.

### 2. Constants (`Constants/`)

This subdirectory centralizes all constant definitions used by the notification system.

-   **Functionality**: Defines enumerations and descriptive metadata for notification parameters, priorities, send methods, template formats, and specific use cases. This ensures standardization and easy management of notification configurations.

### 3. Models (`Models/`)

This subdirectory defines the data structures and associated validation logic for notifications.

-   **Functionality**: Provides a generic `NotificationData` model, specialized models for `EmailNotificationData` and `SmsNotificationData`, along with their respective validation rules. It also includes mapping logic to convert generic `NotificationData` into specific notification types.

### 4. Services (`Services/`)

This subdirectory defines the core service interfaces for handling notification delivery.

-   **Functionality**: Contains interfaces such as `IEmailSenderService`, `ISmsSenderService`, and a generic `INotificationService`. These interfaces abstract the underlying implementation details of sending notifications, promoting a modular and extensible architecture. The `INotificationService` acts as a facade, dispatching notifications to the appropriate sender services.
