using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;
using ReSys.Core.Domain.Promotions.Actions;

namespace ReSys.Core.Domain.Promotions.Promotions;

/// <summary>
/// Configures the database mapping for the <see cref="Promotion"/> entity.
/// </summary>
public sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    /// <summary>
    /// Configures the entity of type <see cref="Promotion"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        #region Table
        // Set the table name for the Promotion entity.
        builder.ToTable(name: Schema.Promotions);
        #endregion

        #region Primary Key
        // Configure the primary key for the Promotion entity.
        builder.HasKey(keyExpression: p => p.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        // Code: Unique index for efficient lookups by coupon code.
        builder.HasIndex(indexExpression: p => p.PromotionCode);
        
        // Active: Index for quickly filtering active promotions.
        builder.HasIndex(indexExpression: p => p.Active);
        builder.HasIndex(indexExpression: p => new { p.StartsAt, p.ExpiresAt, p.Active }); // Index for date range queries
        #endregion

        #region Properties
        // Configure individual properties of the Promotion entity.
        builder.Property(propertyExpression: p => p.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the promotion.");

        // Apply common configurations using extension methods.
        builder.ConfigureUniqueName(); // For IHasUniqueName

        builder.Property(propertyExpression: p => p.PromotionCode)
            .HasMaxLength(maxLength: Promotion.Constraints.CodeMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Code: Optional coupon code for the promotion.");

        builder.Property(propertyExpression: p => p.Description)
            .ConfigureDescriptionOptional(isRequired: false)
            .HasComment(comment: "Description: Optional detailed description of the promotion.");

        builder.Property(propertyExpression: p => p.MinimumOrderAmount)
            .HasPrecision(precision: 18, scale: 4)
            .IsRequired(required: false)
            .HasComment(comment: "MinimumOrderAmount: Minimum order amount required for the promotion to apply.");

        builder.Property(propertyExpression: p => p.MaximumDiscountAmount)
            .HasPrecision(precision: 18, scale: 4)
            .IsRequired(required: false)
            .HasComment(comment: "MaximumDiscountAmount: Maximum discount amount that can be applied by the promotion.");

        builder.Property(propertyExpression: p => p.StartsAt)
            .IsRequired(required: false)
            .HasComment(comment: "StartsAt: Optional start date/time when the promotion becomes active.");

        builder.Property(propertyExpression: p => p.ExpiresAt)
            .IsRequired(required: false)
            .HasComment(comment: "ExpiresAt: Optional expiration date/time when the promotion ends.");

        builder.Property(propertyExpression: p => p.UsageLimit)
            .IsRequired(required: false)
            .HasComment(comment: "UsageLimit: Optional maximum number of times the promotion can be used.");

        builder.Property(propertyExpression: p => p.UsageCount)
            .IsRequired()
            .HasDefaultValue(value: 0)
            .HasComment(comment: "UsageCount: Number of times the promotion has been used.");

        builder.Property(propertyExpression: p => p.Active)
            .IsRequired()
            .HasDefaultValue(value: true)
            .HasComment(comment: "Active: Indicates if the promotion is manually activated/deactivated.");

        builder.Property(propertyExpression: p => p.RequiresCouponCode)
            .IsRequired()
            .HasDefaultValue(value: false)
            .HasComment(comment: "RequiresCouponCode: Indicates if a coupon code must be entered to use this promotion.");

        // Action: A promotion has one action that defines what it does.
        builder.HasOne(p => p.Action)
            .WithOne(a => a.Promotion)
            .HasForeignKey<PromotionAction>(a => a.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);


        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Define navigation properties and their foreign key constraints.

        // Rules: A promotion has many rules that define its eligibility.
        builder.HasMany(navigationExpression: p => p.PromotionRules)
            .WithOne(navigationExpression: r => r.Promotion)
            .HasForeignKey(foreignKeyExpression: r => r.PromotionId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        // Orders: A promotion can be applied to many orders.
        builder.HasMany(navigationExpression: p => p.Orders)
            .WithOne(navigationExpression: o => o.Promotion)
            .HasForeignKey(foreignKeyExpression: o => o.PromotionId)
            .IsRequired(required: false)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull); // Keep the order but remove the promotion link if promotion is deleted.
        #endregion

        #region Ignored Properties
        // Ignored Properties: Properties that are part of the domain model but not mapped to the database.
        builder.Ignore(propertyExpression: p => p.Type);
        builder.Ignore(propertyExpression: p => p.IsActive);
        builder.Ignore(propertyExpression: p => p.IsExpired);
        builder.Ignore(propertyExpression: p => p.HasUsageLimit);
        builder.Ignore(propertyExpression: p => p.RemainingUsage);
        #endregion
    }
}