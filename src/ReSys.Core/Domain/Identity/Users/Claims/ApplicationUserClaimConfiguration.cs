using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Identity.Users.Claims;
/// <summary>
/// Configures the database mapping for the <see cref="UserClaim"/> entity.
/// </summary>
public class ApplicationUserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
{
    /// <summary>
    /// Configures the entity of type <see cref="UserClaim"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        #region Table
        // Set the table name for user claims.
        builder.ToTable(name: Schema.UserClaims);
        #endregion

        #region Primary Key
        // The primary key (Id) and other properties are inherited from IdentityUserClaim<string>
        // and are configured by default by ASP.NET Core Identity.
        // builder.HasKey(keyExpression: uc => uc.Id); // Removed explicit primary key configuration
        #endregion

        #region Properties
        // builder.Property(propertyExpression: uc => uc.Id) // Removed explicit Id property configuration
        //     .ValueGeneratedNever()
        //     .HasComment(comment: "Id: Unique identifier for the user claim. Value generated never.");
        #endregion

        // Apply common configurations using extension methods.
        builder.ConfigureAssignable(); // Configures properties related to assignable entities.
    }
}