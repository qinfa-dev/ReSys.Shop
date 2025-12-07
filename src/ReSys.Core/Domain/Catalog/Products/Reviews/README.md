# Catalog.Products.Reviews Bounded Context

This document describes the `Catalog.Products.Reviews` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages user-generated feedback and ratings for products. It provides mechanisms for customers to submit reviews, for system administrators to moderate them, and for displaying aggregated review data, thereby enhancing product discoverability and building customer trust.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Products.Reviews` bounded context.

-   **Review**: A piece of feedback provided by a user about a specific product, including a rating, optional title, and comment. Represented by the <see cref="Review"/> entity.
-   **Product**: The item being reviewed. (Referenced from `Catalog.Products` Bounded Context).
-   **User**: The individual who submitted the review. (Referenced from `Identity.Users` Bounded Context).
-   **Rating**: A numerical score (e.g., 1-5 stars) indicating the user's satisfaction with the product.
-   **Title**: A short, descriptive heading for the review.
-   **Comment**: The main body of the review, providing detailed feedback.
-   **Status**: The current moderation state of the review (<see cref="ReviewStatus.Pending"/>, <see cref="ReviewStatus.Approved"/>, <see cref="ReviewStatus.Rejected"/>).
-   **Helpfulness Score**: A calculated score indicating how helpful other users found the review (<see cref="Review.HelpfulnessScore"/>).
-   **Helpful Count**: The number of users who found the review helpful.
-   **Not Helpful Count**: The number of users who found the review not helpful.
-   **Is Verified Purchase**: A flag indicating if the review was submitted by a user who purchased the product.
-   **Order ID**: The ID of the order associated with the verified purchase.
-   **Moderated By**: The user who performed the moderation action (approval or rejection).
-   **Moderated At**: The timestamp when the moderation action occurred.
-   **Moderation Notes**: Optional notes or reasons provided by the moderator.

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

-   A <see cref="Review"/> must always be associated with a valid <c>ProductId</c> and <c>UserId</c>.
-   <c>Rating</c> must be within a predefined range (<see cref="Review.Constraints.RatingMinValue"/> to <see cref="Review.Constraints.RatingMaxValue"/>). Invalid ratings are prevented during <see cref="Review.Create(Guid, string, int, string?, string?)"/>.
-   <c>Title</c> and <c>Comment</c> must adhere to maximum length constraints (<see cref="Review.Constraints.TitleMaxLength"/> and <see cref="Review.Constraints.CommentMaxLength"/>, respectively).
-   All new reviews are created with a <see cref="ReviewStatus.Pending"/> status, requiring moderation before public display.
-   When rejecting a review, a reason for rejection (<c>ModerationNotes</c>) is mandatory (<see cref="Review.Reject(string, string)"/>).
-   <see cref="Review"/> instances track their creation and update timestamps (<c>CreatedAt</c>, <c>UpdatedAt</c>), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`Review` to `Product`**: Many-to-one relationship. `Review` is owned by `Product` (from `Catalog.Products`).
-   **`Review` to `ApplicationUser`**: Many-to-one relationship. `Review` links to `ApplicationUser` (from `Identity.Users`).
-   **Shared Kernel**: `Review` inherits from `AuditableEntity<Guid>` (from `SharedKernel.Domain.Entities`), leveraging common patterns for auditing. It uses `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Review**: Submit a new product review using <see cref="Review.Create(Guid, string, int, string?, string?)"/> with a product ID, user ID, rating, and optional title/comment. The review is initially set to <see cref="ReviewStatus.Pending"/>.
-   **Moderate Reviews**:
    -   <see cref="Review.Approve(string, string?)"/>: Change the status of a <see cref="ReviewStatus.Pending"/> review to <see cref="ReviewStatus.Approved"/> by a moderator, making it visible to other users.
    -   <see cref="Review.Reject(string, string)"/>: Change the status of a <see cref="ReviewStatus.Pending"/> review to <see cref="ReviewStatus.Rejected"/> by a moderator, preventing its public display. A reason for rejection is mandatory.
-   **Vote on Review Helpfulness**: Allow users to vote on a review's helpfulness using <see cref="Review.VoteHelpful(bool)"/>. This increments either the <c>HelpfulCount</c> or <c>NotHelpfulCount</c>.
-   **Calculate Helpfulness Score**: The computed property <see cref="Review.HelpfulnessScore"/> provides an aggregated metric of a review's perceived usefulness.

---

## 📝 Considerations / Notes

-   `Review` acts as a child entity within the `Product` aggregate, and its lifecycle is managed by the `Product` aggregate or an application service coordinating with it.
-   The moderation workflow (`Pending`, `Approved`, `Rejected`) is a core aspect of this domain, ensuring quality control over user-generated content.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
