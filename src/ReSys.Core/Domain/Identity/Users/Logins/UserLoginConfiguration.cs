namespace ReSys.Core.Domain.Identity.Users.Logins;

/// <summary>
/// Configures the database mapping for the <see cref="UserLogin"/> entity.
/// </summary>
public sealed class UserLoginConfiguration : IEntityTypeConfiguration<UserLogin>
{
    /// <summary>
    /// Configures the entity of type <see cref="UserLogin"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<UserLogin> builder)
    {
        #region Table
        // Set the table name for user logins.
        builder.ToTable(name: Schema.UserLogins);
        #endregion

        #region Primary Key
        // The composite primary key (LoginProvider, ProviderKey) and other properties are inherited from IdentityUserLogin<string>
        // and are configured by default by ASP.NET Core Identity.
        //builder.HasKey(keyExpression: ul => new { ul.LoginProvider, ul.ProviderKey });
        #endregion

        #region Properties
        //builder.Property(propertyExpression: ul => ul.UserId)
        //    .HasComment(comment: "UserId: Foreign key to the associated ApplicationUser.");
        //builder.Property(propertyExpression: ul => ul.LoginProvider)
        //    .HasComment(comment: "LoginProvider: The login provider for the user (e.g., 'Google', 'Facebook').");
        //builder.Property(propertyExpression: ul => ul.ProviderKey)
        //    .HasComment(comment: "ProviderKey: The unique key from the login provider for the user.");
        //builder.Property(propertyExpression: ul => ul.ProviderDisplayName)
        //    .HasComment(comment: "ProviderDisplayName: The display name for the login provider.");
        #endregion
    }
}