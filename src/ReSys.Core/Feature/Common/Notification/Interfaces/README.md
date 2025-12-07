# Notification Services

This directory defines the core service interfaces for handling various types of notifications within the application. These interfaces abstract the underlying implementation details of sending emails, SMS messages, and other notification types, allowing for a flexible and extensible notification system.

---

## Service Interfaces

### 1. `IEmailSenderService.cs`

This interface defines the contract for services responsible for sending email notifications.

-   **Purpose**: To decouple the email sending logic from the application's core business logic, allowing for different email providers or implementations to be swapped out easily.
-   **Method**:
    -   `Task<ErrorOr<Success>> AddEmailNotificationAsync(EmailNotificationData notificationData, CancellationToken cancellationToken = default)`: Adds an email notification to be sent, taking an `EmailNotificationData` object which contains all necessary details for the email.

### 2. `INotificationService.cs`

This is the primary interface for adding any type of notification to the system. It acts as a facade, delegating the actual sending to specific sender services based on the notification's configuration.

-   **Purpose**: To provide a unified entry point for the application to request notifications, regardless of their eventual delivery method. It orchestrates the process of preparing and dispatching notifications.
-   **Method**:
    -   `Task<ErrorOr<Success>> AddNotificationAsync(NotificationData notification, CancellationToken cancellationToken)`: Adds a generic `NotificationData` object to the system, which will then be processed and sent via the appropriate channel.

### 3. `ISmsSenderService.cs`

This interface defines the contract for services responsible for sending SMS (Short Message Service) notifications.

-   **Purpose**: To abstract the SMS sending mechanism, allowing for integration with various SMS gateway providers.
-   **Method**:
    -   `Task<ErrorOr<Success>> AddSmsNotificationAsync(SmsNotificationData notificationData, CancellationToken cancellationToken = default)`: Adds an SMS notification to be sent, taking an `SmsNotificationData` object which contains all necessary details for the SMS.

---

## Architecture

The notification services are designed to work together:

1.  The application creates a generic `NotificationData` object (often using `NotificationDataBuilder`).
2.  It then calls `INotificationService.AddNotificationAsync` with this `NotificationData`.
3.  The `INotificationService` internally determines the appropriate `IEmailSenderService`, `ISmsSenderService`, or other sender service based on the `NotificationData.SendMethodType`.
4.  The chosen sender service then handles the actual transmission of the notification.

This layered approach ensures a clean separation of concerns and makes the notification system highly modular and testable.
