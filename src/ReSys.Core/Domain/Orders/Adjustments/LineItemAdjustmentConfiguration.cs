//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;

//using ReSys.Core.Common.Domain.Concerns;
//using ReSys.Core.Domain.Constants;

//namespace ReSys.Core.Domain.Orders.Adjustments;

///// <summary>
///// Configures the database mapping for the <see cref="LineItemAdjustment"/> entity.
///// </summary>
//public sealed class LineItemAdjustmentConfiguration : IEntityTypeConfiguration<LineItemAdjustment>
//{
//    /// <summary>
//    /// Configures the entity of type <see cref="LineItemAdjustment"/>.
//    /// </summary>
//    /// <param name="builder">The builder to be used to configure the entity type.</param>
//    public void Configure(EntityTypeBuilder<LineItemAdjustment> builder)
//    {
//        #region Table
//        // Set the table name for the LineItemAdjustment entity.
//        builder.ToTable(name: Schema.LineItemAdjustments);
//        #endregion

//        #region Primary Key
//        // Configure the primary key for the LineItemAdjustment entity.
//        builder.HasKey(keyExpression: lia => lia.Id);
//        #endregion

//        #region Properties
//        // Configure properties for the LineItemAdjustment entity.
//        builder.Property(propertyExpression: lia => lia.Id)
//            .HasColumnName(name: "id")
//            .ValueGeneratedNever()
//            .HasComment(comment: "Id: Unique identifier for the line item adjustment. Value generated never.");

//        builder.Property(propertyExpression: lia => lia.LineItemId)
//            .IsRequired()
//            .HasComment(comment: "LineItemId: Foreign key to the associated LineItem.");

//        builder.Property(propertyExpression: lia => lia.PromotionId)
//            .IsRequired(required: false)
//            .HasComment(comment: "PromotionId: Foreign key to the associated Promotion.");

//        builder.Property(propertyExpression: lia => lia.AmountCents)
//            .IsRequired()
//            .HasComment(comment: "AmountCents: The adjustment amount in cents.");

//        builder.Property(propertyExpression: lia => lia.Description)
//            .HasMaxLength(maxLength: LineItemAdjustment.Constraints.DescriptionMaxLength)
//            .IsRequired()
//            .HasComment(comment: "Description: Description of the adjustment.");

//        // Apply common configurations using extension methods.
//        builder.ConfigureAuditable();
//        #endregion

//        #region Relationships
//        // Configure relationships for the LineItemAdjustment entity.
//        builder.HasOne(navigationExpression: lia => lia.LineItem)
//            .WithMany(navigationExpression: li => li.Adjustments)
//            .HasForeignKey(foreignKeyExpression: lia => lia.LineItemId)
//            .OnDelete(deleteBehavior: DeleteBehavior.Restrict); // Restrict to prevent deleting line item if adjustments exist

//        builder.HasOne(navigationExpression: lia => lia.Promotion)
//            .WithMany(navigationExpression: p => p.LineItemAdjustments)
//            .HasForeignKey(foreignKeyExpression: lia => lia.PromotionId)
//            .IsRequired(required: false)
//            .OnDelete(deleteBehavior: DeleteBehavior.SetNull); // Promotion can be deleted, but adjustment remains
//        #endregion

//        #region Indexes
//        // Configure indexes for frequently queried columns to improve performance.
//        builder.HasIndex(indexExpression: lia => lia.LineItemId);
//        builder.HasIndex(indexExpression: lia => lia.PromotionId);
//        #endregion

//        #region Ignored Properties
//        // Ignore domain-specific or computed properties that should not be mapped to the database.
//        builder.Ignore(propertyExpression: lia => lia.IsPromotion);
//        #endregion
//    }
//}
