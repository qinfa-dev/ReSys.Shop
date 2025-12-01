# Catalog.Products.Reviews Bounded Context

This document describes the `Catalog.Products.Reviews` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages user-generated feedback and ratings for products. It provides mechanisms for customers to submit reviews, for system administrators to moderate them, and for displaying aggregated review data, thereby enhancing product discoverability and building customer trust.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Products.Reviews` bounded context.

-   **Review**: A piece of feedback provided by a user about a specific product, including a rating, optional title, and comment. Represented by the `Review` entity.
-   **Product**: The item being reviewed. (Referenced from `Catalog.Products` Bounded Context).
-   **User**: The individual who submitted the review. (Referenced from `Identity.Users` Bounded Context).
-   **Rating**: A numerical score (e.g., 1-5 stars) indicating the user's satisfaction with the product.
-   **Title**: A short, descriptive heading for the review.
-   **Comment**: The main body of the review, providing detailed feedback.
-   **Status**: The current moderation state of the review (e.g., `Pending`, `Approved`, `Rejected`).

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `Review` is an entity that is owned by the `Product` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`Review`**: This is the central entity of this bounded context. It represents a single customer review for a product. It is an `AuditableEntity`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `ProductId`, `UserId`, `Rating`, `Title`, `Comment`, and `Status` are intrinsic attributes of the `Review` entity.
    -   **Enums**: `ReviewStatus` defines the moderation states for reviews.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Products.Reviews` bounded context.

-   A `Review` must always be associated with a valid `ProductId` and `UserId`.
-   `Rating` must be within a predefined range (e.g., 1 to 5).
-   `Title` and `Comment` must adhere to maximum length constraints.
-   All new reviews are created with a `Pending` status, requiring moderation before public display.
-   `Review` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`Review` to `Product`**: Many-to-one relationship. `Review` is owned by `Product` (from `Catalog.Products`).
-   **`Review` to `ApplicationUser`**: Many-to-one relationship. `Review` links to `ApplicationUser` (from `Identity.Users`).
-   **Shared Kernel**: `Review` inherits from `AuditableEntity<Guid>` (from `SharedKernel.Domain.Entities`), leveraging common patterns for auditing. It uses `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Review**: Instantiate a new `Review` for a product by a user, including rating, title, and comment, and set its initial status to `Pending`.
-   **Approve Review**: Change the status of a `Pending` review to `Approved`, making it visible to other users.
-   **Reject Review**: Change the status of a `Pending` review to `Rejected`, preventing its public display.

---

## 📝 Considerations / Notes

-   `Review` acts as a child entity within the `Product` aggregate, and its lifecycle is managed by the `Product` aggregate or an application service coordinating with it.
-   The moderation workflow (`Pending`, `Approved`, `Rejected`) is a core aspect of this domain, ensuring quality control over user-generated content.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
