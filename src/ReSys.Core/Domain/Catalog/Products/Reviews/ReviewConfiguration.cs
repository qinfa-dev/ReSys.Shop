using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Catalog.Products.Reviews;

/// <summary>
/// Configures the database mapping for the <see cref="Review"/> entity.
/// </summary>
public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    /// <summary>
    /// Configures the entity of type <see cref="Review"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        #region Table
        // Set the table name for the Review entity.
        builder.ToTable(name: Schema.Reviews);
        #endregion

        #region Primary Key
        // Configure the primary key for the Review entity.
        builder.HasKey(keyExpression: r => r.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: r => r.ProductId);
        builder.HasIndex(indexExpression: r => r.UserId);
        builder.HasIndex(indexExpression: r => r.Status);
        #endregion

        #region Properties
        // Configure properties for the Review entity.
        builder.Property(propertyExpression: r => r.Id)
            .HasColumnName(name: "id")
            .HasComment(comment: "Id: Unique identifier for the review. Value generated never.");

        builder.Property(propertyExpression: r => r.ProductId)
            .IsRequired()
            .HasComment(comment: "ProductId: Foreign key to the associated Product.");

        builder.Property(propertyExpression: r => r.UserId)
            .IsRequired()
            .HasComment(comment: "UserId: Foreign key to the associated ApplicationUser.");

        builder.Property(propertyExpression: r => r.Rating)
            .IsRequired()
            .HasComment(comment: "Rating: The star rating given by the user (e.g., 1-5).");

        builder.Property(propertyExpression: r => r.Title)
            .HasMaxLength(maxLength: Review.Constraints.TitleMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Title: A short title for the review.");

        builder.Property(propertyExpression: r => r.Comment)
            .HasMaxLength(maxLength: Review.Constraints.CommentMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Comment: The detailed review text provided by the user.");

        builder.Property(propertyExpression: r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(maxLength: 50)
            .IsRequired()
            .HasComment(comment: "Status: The current moderation status of the review (e.g., Pending, Approved, Rejected).");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the Review entity.
        builder.HasOne(navigationExpression: r => r.Product)
            .WithMany(navigationExpression: p => p.Reviews)
            .HasForeignKey(foreignKeyExpression: r => r.ProductId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: r => r.User)
            .WithMany(navigationExpression: m => m.Reviews)
            .HasForeignKey(foreignKeyExpression: r => r.UserId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}
