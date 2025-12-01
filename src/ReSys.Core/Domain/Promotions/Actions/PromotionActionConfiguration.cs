using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Promotions.Actions;

/// <summary>
/// Configures the database mapping for the <see cref="PromotionAction"/> entity.
/// </summary>
public sealed class PromotionActionConfiguration : IEntityTypeConfiguration<PromotionAction>
{
    /// <summary>
    /// Configures the entity of type <see cref="PromotionAction"/>.
    /// </summary>
    /// <param name="builder">The builder to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<PromotionAction> builder)
    {
        #region Table
        builder.ToTable(name: Schema.Promotions);
        #endregion

        #region Primary Key
        builder.HasKey(a => a.Id);
        #endregion

        #region Properties
        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasComment("Id: Unique identifier for the promotion action.");

        builder.Property(a => a.PromotionId)
            .IsRequired()
            .HasComment("PromotionId: Foreign key linking this action to a promotion.");

        builder.Property(a => a.Type)
            .IsRequired()
            .HasComment("Type: Indicates which type of action is applied.");

        builder.Property(a => a.PublicMetadata)
            .HasColumnType("jsonb")
            .IsRequired(false)
            .HasComment("PublicMetadata: Optional metadata that can be exposed outside the system.");

        builder.Property(a => a.PrivateMetadata)
            .HasColumnType("jsonb")
            .IsRequired(false)
            .HasComment("PrivateMetadata: Optional metadata containing internal configuration for the action.");

        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        builder.HasOne(a => a.Promotion)
            .WithOne(p => p.Action)
            .HasForeignKey<PromotionAction>(a => a.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);
        #endregion

        #region Ignored Properties
        // No ignored properties.
        #endregion
    }
}
