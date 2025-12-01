using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Identity.Users;

/// <summary>
/// Configures the database mapping for the <see cref="ApplicationUser"/> entity.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    /// <summary>
    /// Configures the entity of type <see cref="ApplicationUser"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Set the table name for the ApplicationUser entity.
        builder.ToTable(name: Schema.Users);

        // Set the primary key for the table.
        builder.HasKey(keyExpression: u => u.Id);

        // Configure indices for frequently queried columns to improve performance.
        // NormalizedEmail and NormalizedUserName are used for case-insensitive lookups.
        builder.HasIndex(indexExpression: e => e.NormalizedEmail).IsUnique();
        builder.HasIndex(indexExpression: e => e.NormalizedUserName).IsUnique();
        builder.HasIndex(indexExpression: e => e.PhoneNumber);

        #region Base Identity Properties

        // Configure UserName property with max length and as required.
        builder.Property(propertyExpression: u => u.UserName)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.UsernameMaxLength)
            .IsRequired()
            .HasComment(comment: "UserName: The user's chosen username.");

        // Configure NormalizedUserName for case-insensitive username lookups.
        builder.Property(propertyExpression: u => u.NormalizedUserName)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.UsernameMaxLength)
            .IsRequired()
            .HasComment(comment: "NormalizedUserName: The normalized username for efficient lookups.");

        // Configure Email property with max length and as required.
        builder.Property(propertyExpression: u => u.Email)
            .HasMaxLength(maxLength: CommonInput.Constraints.Email.MaxLength)
            .IsRequired()
            .HasComment(comment: "Email: The user's email address.");

        // Configure NormalizedEmail for case-insensitive email lookups.
        builder.Property(propertyExpression: u => u.NormalizedEmail)
            .HasMaxLength(maxLength: CommonInput.Constraints.Email.MaxLength)
            .IsRequired()
            .HasComment(comment: "NormalizedEmail: The normalized email for efficient lookups.");

        // Configure PhoneNumber property with max length, not required.
        builder.Property(propertyExpression: u => u.PhoneNumber)
            .HasMaxLength(maxLength: CommonInput.Constraints.PhoneNumbers.E164MaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "PhoneNumber: The user's phone number.");

        #endregion

        #region Custom Profile Properties

        // Configure FirstName property.
        builder.Property(propertyExpression: u => u.FirstName)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "FirstName: The user's first name.");

        // Configure LastName property.
        builder.Property(propertyExpression: u => u.LastName)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "LastName: The user's last name.");

        // Configure DateOfBirth property.
        builder.Property(propertyExpression: u => u.DateOfBirth)
            .IsRequired(required: false)
            .HasComment(comment: "DateOfBirth: The user's date of birth.");

        // Configure ProfileImagePath property.
        builder.Property(propertyExpression: u => u.ProfileImagePath)
            .HasMaxLength(maxLength: CommonInput.Constraints.UrlAndUri.UrlMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "ProfileImagePath: The path to the user's profile image.");

        #endregion

        #region Sign-In Tracking

        // Configure LastSignInAt for tracking user's last sign-in time.
        builder.Property(propertyExpression: u => u.LastSignInAt)
            .IsRequired(required: false)
            .HasComment(comment: "LastSignInAt: The timestamp of the user's last sign-in.");

        // Configure LastSignInIp for tracking user's last sign-in IP address.
        builder.Property(propertyExpression: u => u.LastSignInIp)
            .HasMaxLength(maxLength: CommonInput.Constraints.Network.IpV4MaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "LastSignInIp: The IP address from which the user last signed in.");

        // Configure CurrentSignInAt for tracking user's current sign-in time.
        builder.Property(propertyExpression: u => u.CurrentSignInAt)
            .IsRequired(required: false)
            .HasComment(comment: "CurrentSignInAt: The timestamp of the user's current sign-in.");

        // Configure CurrentSignInIp for tracking user's current sign-in IP address.
        builder.Property(propertyExpression: u => u.CurrentSignInIp)
            .HasMaxLength(maxLength: CommonInput.Constraints.Network.IpV4MaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "CurrentSignInIp: The IP address from which the user is currently signed in.");

        // Configure SignInCount to track the number of times a user has signed in.
        builder.Property(propertyExpression: u => u.SignInCount)
            .IsRequired()
            .HasDefaultValue(value: 0)
            .HasComment(comment: "SignInCount: The total number of times the user has signed in.");

        #endregion

        #region Auditable Properties
        builder.ConfigureAuditable();
        #endregion

        #region Concurrency
        builder.ConfigureVersion();
        #endregion

        #region Ignored Properties

        // Ignore domain-specific or computed properties that should not be mapped to the database.
        builder.Ignore(propertyExpression: u => u.FullName);
        builder.Ignore(propertyExpression: u => u.HasProfile);
        builder.Ignore(propertyExpression: u => u.IsActive);
        builder.Ignore(propertyExpression: u => u.DefaultBillingAddress);
        builder.Ignore(propertyExpression: u => u.DefaultShippingAddress);
        builder.Ignore(propertyExpression: u => u.DomainEvents);

        #endregion

        #region Relationships

        // Configure the one-to-many relationship between User and its Claims.
        builder.HasMany(navigationExpression: e => e.Claims)
            .WithOne(navigationExpression: e => e.ApplicationUser)
            .HasForeignKey(foreignKeyExpression: uc => uc.UserId)
            .IsRequired();

        // Configure the one-to-many relationship between User and its Logins.
        builder.HasMany(navigationExpression: e => e.UserLogins)
            .WithOne(navigationExpression: e => e.ApplicationUser)
            .HasForeignKey(foreignKeyExpression: ul => ul.UserId)
            .IsRequired();

        // Configure the one-to-many relationship between User and its Tokens.
        builder.HasMany(navigationExpression: e => e.UserTokens)
            .WithOne(navigationExpression: e => e.ApplicationUser)
            .HasForeignKey(foreignKeyExpression: ut => ut.UserId)
            .IsRequired();

        // Configure the many-to-many relationship between Users and Roles through the ApplicationUserRole join table.
        builder.HasMany(navigationExpression: e => e.UserRoles)
            .WithOne(navigationExpression: e => e.ApplicationUser)
            .HasForeignKey(foreignKeyExpression: ur => ur.UserId)
            .IsRequired();

        // Configure the one-to-many relationship for RefreshTokens, with cascade delete.
        builder.HasMany(navigationExpression: u => u.RefreshTokens)
            .WithOne(navigationExpression: rt => rt.ApplicationUser)
            .HasForeignKey(foreignKeyExpression: rt => rt.UserId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade); // When a user is deleted, their refresh tokens are also deleted.

        // Configure the one-to-many relationship for UserAddresses, with cascade delete.
        builder.HasMany(navigationExpression: u => u.UserAddresses)
            .WithOne(navigationExpression: ua => ua.ApplicationUser)
            .HasForeignKey(foreignKeyExpression: ua => ua.UserId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade); // When a user is deleted, their address associations are also deleted.

        #endregion
    }
}
