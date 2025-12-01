using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Identity.Tokens;

/// <summary>
/// Configures the database mapping for the <see cref="RefreshToken"/> entity.
/// </summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <summary>
    /// Configures the entity of type <see cref="RefreshToken"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        #region Table
        // Set the table name for refresh tokens.
        builder.ToTable(name: Schema.RefreshTokens);
        #endregion

        #region Primary Key
        // Set the primary key for the table.
        builder.HasKey(keyExpression: e => e.Id);
        #endregion

        #region Indexes
        // A unique index on the token hash to ensure token uniqueness and fast lookups.
        builder.HasIndex(indexExpression: e => e.TokenHash).IsUnique();
        // An index on UserId for efficient querying of tokens by user.
        builder.HasIndex(indexExpression: e => e.UserId);
        #endregion

        #region Properties
        // Configure properties for the RefreshToken entity.
        builder.Property(propertyExpression: e => e.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the refresh token. Value generated never.");

        // The hashed version of the refresh token.
        builder.Property(propertyExpression: e => e.TokenHash)
            .IsRequired()
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.ShortTextMaxLength)
            .HasComment(comment: "TokenHash: The hashed version of the refresh token.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        builder.ConfigureAssignable();

        // The IP address from which the token was created.
        builder.Property(propertyExpression: e => e.CreatedByIp)
            .IsRequired()
            .HasMaxLength(maxLength: CommonInput.Constraints.Network.IpV4MaxLength)
            .HasComment(comment: "CreatedByIp: The IP address from which the token was created.");

        // The IP address from which the token was revoked, if applicable.
        builder.Property(propertyExpression: e => e.RevokedByIp)
            .HasMaxLength(maxLength: CommonInput.Constraints.Network.IpV4MaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "RevokedByIp: The IP address from which the token was revoked, if applicable.");

        builder.Property(propertyExpression: e => e.ExpiresAt)
            .IsRequired()
            .HasComment(comment: "ExpiresAt: The expiration date and time of the refresh token.");

        builder.Property(propertyExpression: e => e.RevokedAt)
            .IsRequired(required: false)
            .HasComment(comment: "RevokedAt: The date and time when the refresh token was revoked.");
        #endregion

        #region Relationships
        // Configure the one-to-many relationship between ApplicationUser and RefreshToken.
        builder.HasOne(navigationExpression: e => e.ApplicationUser)
            .WithMany(navigationExpression: u => u.RefreshTokens)
            .HasForeignKey(foreignKeyExpression: e => e.UserId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade); // When a user is deleted, their refresh tokens are also deleted.
        #endregion
    }
}
