using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Identity.Users;

namespace ReSys.Core.Domain.Catalog.Products.Reviews;

public sealed class Review : AuditableEntity<Guid>
{
    #region Contraints
    public static class Constraints
    {
        public const int RatingMinValue = 1;
        public const int RatingMaxValue = 5;
        public const int TitleMaxLength = 100;
        public const int CommentMaxLength = 1000;
    }
    public enum ReviewStatus
    {
        Pending,
        Approved,
        Rejected
    }
    #endregion

    #region Properties
    public Guid ProductId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string? Comment { get; init; }
    public ReviewStatus Status { get; set;}
    public string? ModeratedBy { get; set; }
    public DateTimeOffset? ModeratedAt { get; set; }
    public string? ModerationNotes { get; set; }
    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public Guid? OrderId { get; set; }
    #endregion

    #region Relationships
    public Product? Product { get; set;}
    public ApplicationUser? User { get; set;}
    #endregion
    #region Computed Properties

    public double HelpfulnessScore
    {
        get
        {
            var total = HelpfulCount + NotHelpfulCount;
            if (total == 0) return 0;
            return (double)HelpfulCount / total;
        }
    }

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

    private Review() { }

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
            Status = ReviewStatus.Pending // Reviews should be pending approval
        };
    }

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
