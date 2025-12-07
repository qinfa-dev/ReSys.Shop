using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Identity.Users;

namespace ReSys.Core.Domain.Catalog.Products.Reviews;

/// <summary>
/// Represents a user-generated review for a product, including a rating, optional title, comment, and moderation status.
/// Reviews are crucial for customer feedback, product discovery, and building trust.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Catalog Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>User Feedback</term>
/// <description>Allows customers to share their experience with a product.</description>
/// </item>
/// <item>
/// <term>Product Rating</term>
/// <description>Provides a numerical score for product quality and satisfaction.</description>
/// </item>
/// <item>
/// <term>Moderation Workflow</term>
/// <description>Ensures review quality and compliance through a pending/approved/rejected process.</description>
/// </item>
/// <item>
/// <term>Helpfulness Tracking</term>
/// <description>Allows other users to vote on the helpfulness of a review.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes:</strong>
/// <list type="bullet">
/// <item>
/// <term>ProductId</term>
/// <description>The associated product.</description>
/// </item>
/// <item>
/// <term>UserId</term>
/// <description>The user who submitted the review.</description>
/// </item>
/// <item>
/// <term>Rating</term>
/// <description>A score from 1 to 5.</description>
/// </item>
/// <item>
/// <term>Status</term>
/// <description>The moderation status (Pending, Approved, Rejected).</description>
/// </item>
/// </list>
/// </para>
/// </remarks>
public sealed class Review : AuditableEntity<Guid>
{
    #region Contraints
    /// <summary>
    /// Defines constraints and limits for <see cref="Review"/> properties.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// The minimum allowed value for a product rating.
        /// </summary>
        public const int RatingMinValue = 1;
        /// <summary>
        /// The maximum allowed value for a product rating.
        /// </summary>
        public const int RatingMaxValue = 5;
        /// <summary>
        /// The maximum allowed length for the review title.
        /// </summary>
        public const int TitleMaxLength = 100;
        /// <summary>
        /// The maximum allowed length for the review comment.
        /// </summary>
        public const int CommentMaxLength = 1000;
    }
    /// <summary>
    /// Defines the moderation status of a product review.
    /// </summary>
    public enum ReviewStatus
    {
        /// <summary>The review is awaiting moderation and is not yet public.</summary>
        Pending,
        /// <summary>The review has been approved and is visible to the public.</summary>
        Approved,
        /// <summary>The review has been rejected and is not visible to the public.</summary>
        Rejected
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets the unique identifier of the product being reviewed.
    /// </summary>
    public Guid ProductId { get; init; }
    /// <summary>
    /// Gets the unique identifier of the user who submitted the review.
    /// </summary>
    public string UserId { get; init; } = string.Empty;
    /// <summary>
    /// Gets the numerical rating given by the user, typically on a scale of 1 to 5.
    /// </summary>
    public int Rating { get; init; }
    /// <summary>
    /// Gets the optional title of the review.
    /// </summary>
    public string? Title { get; init; }
    /// <summary>
    /// Gets the optional detailed comment of the review.
    /// </summary>
    public string? Comment { get; init; }
    /// <summary>
    /// Gets or sets the current moderation status of the review (e.g., Pending, Approved, Rejected).
    /// </summary>
    public ReviewStatus Status { get; set;}
    /// <summary>
    /// Gets or sets the identifier of the user who moderated this review.
    /// </summary>
    public string? ModeratedBy { get; set; }
    /// <summary>
    /// Gets or sets the timestamp when the review was last moderated.
    /// </summary>
    public DateTimeOffset? ModeratedAt { get; set; }
    /// <summary>
    /// Gets or sets any notes or reasons provided during moderation.
    /// </summary>
    public string? ModerationNotes { get; set; }
    /// <summary>
    /// Gets or sets the count of users who found this review helpful.
    /// </summary>
    public int HelpfulCount { get; set; }
    /// <summary>
    /// Gets or sets the count of users who found this review not helpful.
    /// </summary>
    public int NotHelpfulCount { get; set; }
    /// <summary>
    /// Indicates if the user who submitted the review is confirmed to have purchased the product.
    /// </summary>
    public bool IsVerifiedPurchase { get; set; }
    /// <summary>
    /// Gets or sets the optional unique identifier of the order associated with this review,
    /// used to verify purchase.
    /// </summary>
    public Guid? OrderId { get; set; }
    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the navigation property to the <see cref="Product"/> being reviewed.
    /// </summary>
    public Product? Product { get; set;}
    /// <summary>
    /// Gets or sets the navigation property to the <see cref="User"/> who submitted the review.
    /// </summary>
    public User? User { get; set;}
    #endregion
    #region Computed Properties

    /// <summary>
    /// Calculates a score indicating how helpful other users found this review.
    /// The score is between 0 and 1, where 1 means all votes were helpful.
    /// Returns 0 if there are no votes.
    /// </summary>
    public double HelpfulnessScore
    {
        get
        {
            var total = HelpfulCount + NotHelpfulCount;
            if (total == 0) return 0;
            return (double)HelpfulCount / total;
        }
    }

    /// <summary>
    /// Records a helpful or not helpful vote for this review.
    /// </summary>
    /// <param name="helpful">True if the vote is helpful, false if not helpful.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Review}"/> result.
    /// Returns the updated <see cref="Review"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method increments either the <see cref="HelpfulCount"/> or <see cref="NotHelpfulCount"/>
    /// and updates the <c>UpdatedAt</c> timestamp.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var review = GetReviewById(reviewId);
    /// var voteResult = review.VoteHelpful(helpful: true);
    /// if (voteResult.IsError)
    /// {
    ///     Console.WriteLine($"Error voting on review: {voteResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Review '{review.Title}' now has {review.HelpfulCount} helpful votes.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public ErrorOr<Review> VoteHelpful(bool helpful)
    {
        if (helpful)
            HelpfulCount++;
        else
            NotHelpfulCount++;

        UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }
    #endregion

    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private Review() { }

    /// <summary>
    /// Factory method to create a new <see cref="Review"/> instance.
    /// New reviews are initially set to <see cref="ReviewStatus.Pending"/> for moderation.
    /// </summary>
    /// <param name="productId">The unique identifier of the product being reviewed.</param>
    /// <param name="userId">The unique identifier of the user submitting the review.</param>
    /// <param name="rating">The numerical rating (1-5) given by the user.</param>
    /// <param name="title">Optional title for the review.</param>
    /// <param name="comment">Optional detailed comment for the review.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Review}"/> result.
    /// Returns the newly created <see cref="Review"/> instance on success.
    /// Returns a list of <see cref="Error"/> if validation fails (e.g., invalid rating, title/comment too long).
    /// </returns>
    /// <remarks>
    /// This method performs validation for rating range and string lengths.
    /// The review's status is automatically set to <see cref="ReviewStatus.Pending"/>.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// Guid productId = Guid.NewGuid(); // Assume existing Product ID
    /// string userId = "user123"; // Assume existing User ID
    /// var reviewResult = Review.Create(
    ///     productId: productId,
    ///     userId: userId,
    ///     rating: 4,
    ///     title: "Great Product!",
    ///     comment: "I really enjoyed using this product. Highly recommend.");
    /// 
    /// if (reviewResult.IsError)
    /// {
    ///     Console.WriteLine($"Error creating review: {reviewResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     var newReview = reviewResult.Value;
    ///     Console.WriteLine($"Review '{newReview.Title}' created with status: {newReview.Status}.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public static ErrorOr<Review> Create(Guid productId, string userId, int rating, string? title = null, string? comment = null)
    {
        List<Error> errors = [];

        if (rating < Constraints.RatingMinValue || rating > Constraints.RatingMaxValue)
        {
            errors.Add(item: CommonInput.Errors.InvalidRange(
                prefix: nameof(Review),
                field: nameof(Rating),
                min: Constraints.RatingMinValue,
                max: Constraints.RatingMaxValue));
        }

        if (title?.Length > Constraints.TitleMaxLength)
        {
            errors.Add(item: CommonInput.Errors.TooLong(
                prefix: nameof(Review),
                field: nameof(Title),
                maxLength: Constraints.TitleMaxLength));
        }

        if (comment?.Length > Constraints.CommentMaxLength)
        {
            errors.Add(item: CommonInput.Errors.TooLong(
                prefix: nameof(Review),
                field: nameof(Comment),
                maxLength: Constraints.CommentMaxLength));
        }

        if (errors.Any())
        {
            return errors;
        }

        return new Review
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            UserId = userId,
            Rating = rating,
            Title = title,
            Comment = comment,
            Status = ReviewStatus.Pending, // Reviews should be pending approval
            CreatedAt = DateTimeOffset.UtcNow // Inherited from AuditableEntity
        };
    }

    /// <summary>
    /// Approves the review, changing its status from <see cref="ReviewStatus.Pending"/> to <see cref="ReviewStatus.Approved"/>.
    /// An approved review becomes visible to the public.
    /// </summary>
    /// <param name="moderatorId">The unique identifier of the user who approved the review.</param>
    /// <param name="notes">Optional notes from the moderator regarding the approval.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Review}"/> result.
    /// Returns the updated <see cref="Review"/> instance on success.
    /// Returns the current review if it is already approved (idempotent).
    /// </returns>
    /// <remarks>
    /// This method updates the <see cref="Status"/>, <see cref="ModeratedBy"/>, <see cref="ModeratedAt"/>,
    /// and <see cref="ModerationNotes"/> properties. The <c>UpdatedAt</c> timestamp is also updated.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var review = GetReviewById(reviewId); // Assume review is Pending
    /// var moderatorId = "adminUser";
    /// var result = review.Approve(moderatorId, "Content looks good.");
    /// if (result.IsError)
    /// {
    ///     Console.WriteLine($"Error approving review: {result.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Review '{review.Title}' approved by {review.ModeratedBy}.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public ErrorOr<Review> Approve(string moderatorId, string? notes = null)
    {
        if (Status == ReviewStatus.Approved) return this;

        Status = ReviewStatus.Approved;
        ModeratedBy = moderatorId;
        ModeratedAt = DateTimeOffset.UtcNow;
        ModerationNotes = notes;
        UpdatedAt = DateTimeOffset.UtcNow;

        return this;
    }

    /// <summary>
    /// Rejects the review, changing its status to <see cref="ReviewStatus.Rejected"/>.
    /// A rejected review is not visible to the public. A reason for rejection is required.
    /// </summary>
    /// <param name="moderatorId">The unique identifier of the user who rejected the review.</param>
    /// <param name="reason">The mandatory reason for rejecting the review.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Review}"/> result.
    /// Returns the updated <see cref="Review"/> instance on success.
    /// Returns a validation error (<c>Review.ReasonRequired</c>) if the reason is null or whitespace.
    /// </returns>
    /// <remarks>
    /// This method updates the <see cref="Status"/>, <see cref="ModeratedBy"/>, <see cref="ModeratedAt"/>,
    /// and <see cref="ModerationNotes"/> properties. The <c>UpdatedAt</c> timestamp is also updated.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var review = GetReviewById(reviewId); // Assume review is Pending
    /// var moderatorId = "adminUser";
    /// var result = review.Reject(moderatorId, "Violates community guidelines.");
    /// if (result.IsError)
    /// {
    ///     Console.WriteLine($"Error rejecting review: {result.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Review '{review.Title}' rejected by {review.ModeratedBy}.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public ErrorOr<Review> Reject(string moderatorId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Error.Validation(
                code: "Review.ReasonRequired",
                description: "Rejection reason is required");

        Status = ReviewStatus.Rejected;
        ModeratedBy = moderatorId;
        ModeratedAt = DateTimeOffset.UtcNow;
        ModerationNotes = reason;
        UpdatedAt = DateTimeOffset.UtcNow;

        return this;
    }
}
