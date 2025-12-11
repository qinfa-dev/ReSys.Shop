namespace ReSys.Core.Domain.Identity.Roles.Claims;
/// <summary>
/// Configures the database mapping for the <see cref="RoleClaim"/> entity.
/// </summary>
public sealed class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    /// <summary>
    /// Configures the entity of type <see cref="RoleClaim"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        #region Table
        // Set the table name for role claims.
        builder.ToTable(name: Schema.RoleClaims);
        
        #endregion

        #region Primary Key
        // The primary key (Id) and other properties are inherited from IdentityRoleClaim<string>
        // and are configured by default by ASP.NET Core Identity.
        // builder.HasKey(keyExpression: rc => rc.Id);
        #endregion

        #region Properties
        // builder.Property(propertyExpression: rc => rc.Id)
        //     .ValueGeneratedNever()
        //     .HasComment(comment: "Id: Unique identifier for the role claim. Value generated never.");
        #endregion

        // Apply common configurations using extension methods.
        builder.ConfigureAssignable(); // Configures properties related to assignable entities.
    }
}
