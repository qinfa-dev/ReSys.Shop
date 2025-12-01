# Notification Builders

This directory contains builder classes and extension methods designed to simplify the creation and configuration of `NotificationData` objects. These builders provide a fluent API, enabling developers to construct complex notification instances with ease and ensuring that all necessary parameters and settings are correctly applied.

---

## Builders

### 1. Notification Data Builder (`Notification.DataBuilder.cs`)

This static class provides a comprehensive fluent API for building `NotificationData` objects. It integrates with the notification constants (use cases, parameters, send methods, formats) to provide default values and ensure consistency.

-   **`WithUseCase(NotificationUseCase useCase)`**: Initializes a new `NotificationData` instance based on a predefined `NotificationUseCase`. It automatically populates default `SendMethodType`, `TemplateFormatType`, `Content`, `HtmlContent`, and `Title` from the `NotificationUseCases.Templates` dictionary.
-   **`WithUseCase(this ErrorOr<NotificationData> result, NotificationUseCase useCase)`**: An extension method to update the use case of an existing `NotificationData` instance, reapplying default template values.
-   **`WithSendMethodType(this ErrorOr<NotificationData> result, NotificationSendMethod sendMethodType)`**: Sets the delivery method for the notification.
-   **`AddParam(this ErrorOr<NotificationData> result, NotificationParameter parameter, string? value)`**: Adds a single dynamic parameter to the notification's `Values` dictionary for template replacement.
-   **`AddParams(this ErrorOr<NotificationData> result, Dictionary<NotificationParameter, string?>? values)`**: Adds multiple dynamic parameters to the notification.
-   **`WithReceivers(this ErrorOr<NotificationData> result, List<string>? receivers)`**: Sets the list of recipients for the notification, ensuring uniqueness and non-empty values.
-   **`WithReceiver(this ErrorOr<NotificationData> Data, string? receiver)`**: Adds a single recipient to the notification.
-   **`WithTitle(this ErrorOr<NotificationData> result, string? title)`**: Sets the title of the notification (primarily for email).
-   **`WithContent(this ErrorOr<NotificationData> result, string? content)`**: Sets the plain text content of the notification.
-   **`WithHtmlContent(this ErrorOr<NotificationData> result, string? htmlContent)`**: Sets the HTML content of the notification (primarily for email).
-   **`WithCreatedBy(this ErrorOr<NotificationData> result, string? createdBy)`**: Sets the creator of the notification.
-   **`WithAttachments(this ErrorOr<NotificationData> result, List<string>? attachments)`**: Adds attachments to the notification (primarily for email).
-   **`WithPriority(this ErrorOr<NotificationData> result, NotificationPriority priority)`**: Sets the priority level of the notification.
-   **`WithLanguage(this ErrorOr<NotificationData> result, string? language)`**: Sets the language of the notification.
-   **`SetCreatedBy(this ErrorOr<NotificationData> result, string? createdBy, DateTimeOffset? createAt = null)`**: Sets the creator and creation timestamp.
-   **`Build(this ErrorOr<NotificationData> result)`**: Finalizes the `NotificationData` instance and performs a final validation.
-   **`CreateSmsNotificationData(...)`**: Helper method to directly create and validate an `SmsNotificationData` instance.
-   **`CreateEmailNotificationData(...)`**: Helper method to directly create and validate an `EmailNotificationData` instance.
-   **`CreateNotificationData(...)`**: Helper method to create and validate a generic `NotificationData` instance.

---

## Purpose

The notification builders are designed to:

-   **Simplify Object Creation**: Provide a clean and readable way to construct `NotificationData` objects, reducing the complexity of direct instantiation.
-   **Enforce Consistency**: Leverage predefined constants and templates to ensure that notifications adhere to application-wide standards.
-   **Improve Developer Experience**: Offer a fluent interface that guides developers through the process of creating valid notification instances.
-   **Centralize Logic**: Consolidate the logic for populating default values and performing initial validation based on the chosen use case.
