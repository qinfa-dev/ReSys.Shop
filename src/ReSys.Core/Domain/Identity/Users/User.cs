using ErrorOr;

using Microsoft.AspNetCore.Identity;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Products.Reviews;
using ReSys.Core.Domain.Identity.Tokens;
using ReSys.Core.Domain.Identity.UserAddresses;
using ReSys.Core.Domain.Identity.Users.Claims;
using ReSys.Core.Domain.Identity.Users.Logins;
using ReSys.Core.Domain.Identity.Users.Roles;
using ReSys.Core.Domain.Identity.Users.Tokens;
using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Payments.PaymentSources;

namespace ReSys.Core.Domain.Identity.Users;

/// <summary>
/// Represents a user account within the ASP.NET Core Identity system, serving as an aggregate root
/// for managing user identity, authentication, profile information, and associated entities.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Identity Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>Central Identity</term>
/// <description>The core entity for any human or system interacting with the application.</description>
/// </item>
/// <item>
/// <term>Authentication</term>
/// <description>Manages credentials, login attempts, and lockout status.</description>
/// </item>
/// <item>
/// <term>Authorization</term>
/// <description>Links to roles and claims that define what the user can access and do.</description>
/// </item>
/// <item>
/// <term>Profile Management</term>
/// <description>Stores personal details, contact information, and preferences.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes:</strong>
/// <list type="bullet">
/// <item>
/// <term>UserName / Email</term>
/// <description>Primary identifiers for authentication and communication.</description>
/// </item>
/// <item>
/// <term>FirstName / LastName</term>
/// <description>Personal details for personalization and display.</description>
/// </item>
/// <item>
/// <term>SignInCount</term>
/// <description>Tracks user engagement.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Concerns Implemented:</strong>
/// <list type="bullet">
/// <item><strong>IHasVersion</strong> - For optimistic concurrency control.</item>
/// <item><strong>IHasDomainEvents</strong> - For publishing domain events on state changes.</item>
/// <item><strong>IHasAuditable</strong> - For tracking creation and update timestamps and by whom.</item>
/// </list>
/// </para>
/// </remarks>
public class User : IdentityUser, IHasVersion, IHasDomainEvents, IHasAuditable
{
    #region Constraints

    /// <summary>
    /// Defines constraints and constant values specific to <see cref="User"/> properties.
    /// These are typically derived from underlying common input constraints.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// Maximum length for user credentials (username, email, phone number).
        /// Derived from the maximums of common input constraints for these fields.
        /// </summary>
        public static int MaxCredentialLength => Math.Max(val1: CommonInput.Constraints.NamesAndUsernames.UsernameMaxLength,
            val2: Math.Max(val1: CommonInput.Constraints.PhoneNumbers.E164MaxLength,
                val2: CommonInput.Constraints.Email.MaxLength));
        /// <summary>
        /// Minimum length for user credentials (username, email, phone number).
        /// Derived from the minimums of common input constraints for these fields.
        /// </summary>
        public static int MinCredentialLength => Math.Max(val1: CommonInput.Constraints.NamesAndUsernames.UsernameMinLength,
            val2: Math.Max(val1: CommonInput.Constraints.PhoneNumbers.MinLength,
                val2: CommonInput.Constraints.Email.MinLength));
    }

    #endregion

    #region Errors

    /// <summary>
    /// Defines domain error scenarios specific to <see cref="User"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a requested user could not be found based on the provided credential.
        /// </summary>
        /// <param name="credential">The credential (e.g., username, email) that was used for the search.</param>
        public static Error NotFound(string credential) => Error.NotFound(code: $"{nameof(User)}.NotFound",
            description: $"User with credential '{credential}' was not found.");
        /// <summary>
        /// Error indicating that a user with the specified username already exists.
        /// </summary>
        /// <param name="userName">The username that caused the conflict.</param>
        public static Error UserNameAlreadyExists(string userName) => Error.Conflict(
            code: $"{nameof(User)}.UserNameAlreadyExists",
            description: $"Username '{userName}' already exists.");
        /// <summary>
        /// Error indicating that a user with the specified email address already exists.
        /// </summary>
        /// <param name="email">The email address that caused the conflict.</param>
        public static Error EmailAlreadyExists(string email) => Error.Conflict(
            code: $"{nameof(User)}.EmailAlreadyExists",
            description: $"Email '{email}' already exists.");
        /// <summary>Error indicating that the user's email address has not been confirmed.</summary>
        public static Error EmailNotConfirmed => Error.Validation(code: $"{nameof(User)}.EmailNotConfirmed",
            description: "Email address is not confirmed.");

        /// <summary>
        /// Error indicating that a user with the specified phone number already exists.
        /// </summary>
        /// <param name="phoneNumber">The phone number that caused the conflict.</param>
        public static Error PhoneNumberAlreadyExists(string phoneNumber) => Error.Conflict(
            code: $"{nameof(User)}.PhoneNumberAlreadyExists",
            description: $"Phone number '{phoneNumber}' already exists.");

        /// <summary>Error indicating that the provided credentials for login are invalid.</summary>
        public static Error InvalidCredentials => Error.Validation(
            code: $"{nameof(User)}.InvalidCredentials",
            description: "Invalid email or password.");
        /// <summary>Error indicating that the user's account is locked out.</summary>
        public static Error LockedOut => Error.Validation(code: $"{nameof(User)}.LockedOut",
            description: "Account is locked out.");

        /// <summary>Error indicating that a provided token (e.g., password reset, email confirmation) is invalid or expired.</summary>
        public static Error InvalidToken => Error.Validation(code: $"{nameof(User)}.InvalidToken",
            description: "Invalid or expired token.");

        /// <summary>Error indicating that a user cannot be deleted because they have active refresh tokens.</summary>
        public static Error HasActiveTokens => Error.Validation(code: $"{nameof(User)}.HasActiveTokens",
            description: "Cannot delete user with active refresh tokens.");
        /// <summary>Error indicating that a user cannot be deleted because they are assigned to roles.</summary>
        public static Error HasActiveRoles => Error.Validation(code: $"{nameof(User)}.HasActiveRoles",
            description: "Cannot delete user with assigned roles.");

        /// <summary>Error indicating that the user is not authorized to access a specific resource.</summary>
        public static Error Unauthorized =>
            Error.Unauthorized(code: $"{nameof(User)}.Unauthorized",
                description: "User is not authorized to access this resource.");
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public string? FirstName { get; set; }
    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public string? LastName { get; set; }
    /// <summary>
    /// Gets or sets the user's date of birth.
    /// </summary>
    public DateTimeOffset? DateOfBirth { get; set; }
    /// <summary>
    /// Gets or sets the path or URL to the user's profile image.
    /// </summary>
    public string? ProfileImagePath { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the user's last recorded sign-in.
    /// </summary>
    public DateTimeOffset? LastSignInAt { get; set; }
    /// <summary>
    /// Gets or sets the IP address from which the user last signed in.
    /// </summary>
    public string? LastSignInIp { get; set; }
    /// <summary>
    /// Gets or sets the UTC timestamp of the user's current or most recent sign-in.
    /// </summary>
    public DateTimeOffset? CurrentSignInAt { get; set; }
    /// <summary>
    /// Gets or sets the IP address from which the user currently or most recently signed in.
    /// </summary>
    public string? CurrentSignInIp { get; set; }
    /// <summary>
    /// Gets or sets the total count of successful sign-ins for this user.
    /// </summary>
    public int SignInCount { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the user account was created.
    /// Inherited from <see cref="IHasAuditable"/>.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>
    /// Gets or sets the UTC timestamp when the user account was last updated.
    /// Inherited from <see cref="IHasAuditable"/>.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the user or system that created the user account.
    /// Inherited from <see cref="IHasAuditable"/>.
    /// </summary>
    public string? CreatedBy { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the user or system that last updated the user account.
    /// Inherited from <see cref="IHasAuditable"/>.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the concurrency token used for optimistic concurrency control.
    /// Inherited from <see cref="IHasVersion"/>.
    /// </summary>
    public long Version { get; set; }

    #endregion

    #region Relationships

    /// <summary>
    /// Gets or sets the collection of <see cref="RefreshToken"/>s associated with this user.
    /// </summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    /// <summary>
    /// Gets or sets the collection of <see cref="UserClaim"/>s associated with this user.
    /// </summary>
    public ICollection<UserClaim> Claims { get; set; } = [];
    /// <summary>
    /// Gets or sets the collection of <see cref="UserLogin"/>s (external logins) associated with this user.
    /// </summary>
    public ICollection<UserLogin> UserLogins { get; set; } = [];
    /// <summary>
    /// Gets or sets the collection of <see cref="UserToken"/>s (e.g., password reset tokens) associated with this user.
    /// </summary>
    public ICollection<UserToken> UserTokens { get; set; } = [];
    /// <summary>
    /// Gets or sets the collection of <see cref="UserRole"/>s, representing the roles assigned to this user.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = [];
    /// <summary>
    /// Gets or sets the collection of <see cref="UserAddress"/>es associated with this user.
    /// </summary>
    public ICollection<UserAddress> UserAddresses { get; set; } = [];
    /// <summary>
    /// Gets or sets the collection of <see cref="Order"/>s placed by this user.
    /// </summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>(); 
    /// <summary>
    /// Gets or sets the collection of <see cref="Review"/>s written by this user.
    /// </summary>
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    /// <summary>
    /// Gets or sets the collection of <see cref="PaymentSource"/>s associated with this user.
    /// </summary>
    public ICollection<PaymentSource> PaymentSources { get; set; } = new List<PaymentSource>();

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the full name of the user by combining <see cref="FirstName"/> and <see cref="LastName"/>.
    /// Returns an empty string if both are null or whitespace.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
    /// <summary>
    /// Indicates whether the user has basic profile information (first or last name) set.
    /// </summary>
    public bool HasProfile => !string.IsNullOrWhiteSpace(value: FirstName) || !string.IsNullOrWhiteSpace(value: LastName);
    /// <summary>
    /// Indicates whether the user account is currently active and not locked out.
    /// Considers <c>LockoutEnabled</c> and <c>LockoutEnd</c> properties.
    /// </summary>
    public bool IsActive => !LockoutEnabled || LockoutEnd == null || LockoutEnd <= DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets the user's default billing address, if one is designated.
    /// </summary>
    public UserAddress? DefaultBillingAddress => UserAddresses.FirstOrDefault(predicate: ua => ua is { IsDefault: true, Type: AddressType.Billing });
    /// <summary>
    /// Gets the user's default shipping address, if one is designated.
    /// </summary>
    public UserAddress? DefaultShippingAddress => UserAddresses.FirstOrDefault(predicate: ua => ua is { IsDefault: true, Type: AddressType.Shipping });
    #endregion

    #region Factory Methods

    /// <summary>
    /// Factory method to create a new <see cref="User"/> instance.
    /// This method initializes the core user properties and sets up default values for Identity-related fields.
    /// </summary>
    /// <param name="email">The user's email address, which is used to set <c>UserName</c> if not provided.</param>
    /// <param name="userName">Optional: The user's unique username. If null, derived from the email prefix.</param>
    /// <param name="firstName">Optional: The user's first name.</param>
    /// <param name="lastName">Optional: The user's last name.</param>
    /// <param name="dateOfBirth">Optional: The user's date of birth.</param>
    /// <param name="phoneNumber">Optional: The user's phone number.</param>
    /// <param name="profileImagePath">Optional: Path or URL to the user's profile image.</param>
    /// <param name="emailConfirmed">Indicates if the email address is confirmed. Defaults to false.</param>
    /// <param name="phoneNumberConfirmed">Indicates if the phone number is confirmed. Defaults to false.</param>
    /// <returns>
    /// An <see cref="ErrorOr{User}"/> result.
    /// Returns the newly created <see cref="User"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method performs basic trimming and normalization of input strings.
    /// It initializes Identity-specific fields like <c>SecurityStamp</c> and <c>ConcurrencyStamp</c>.
    /// A <see cref="Events.UserCreated"/> domain event is added, which can be extended by a <see cref="Events.UserRegistered"/> event.
    /// </remarks>
    public static ErrorOr<User> Create(
       string? email,
       string? userName = null,
       string? firstName = null,
       string? lastName = null,
       DateTimeOffset? dateOfBirth = null,
       string? phoneNumber = null,
       string? profileImagePath = null,
       bool emailConfirmed = false,
       bool phoneNumberConfirmed = false)
    {
        string trimmedEmail = email?.Trim() ?? string.Empty;

        string effectiveUserName = string.IsNullOrWhiteSpace(value: userName) ? trimmedEmail.Split(separator: "@").First() : userName.Trim();

        User user = new()
        {
            Id = Guid.NewGuid().ToString(), // IdentityUser expects string ID
            Email = trimmedEmail,
            NormalizedEmail = trimmedEmail.ToUpperInvariant(),
            EmailConfirmed = emailConfirmed,
            UserName = effectiveUserName,
            NormalizedUserName = effectiveUserName.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(format: "N"),
            ConcurrencyStamp = Guid.NewGuid().ToString(format: "N"),
            LockoutEnabled = true, // Default to true for security, can be changed
            FirstName = firstName?.Trim(),
            LastName = lastName?.Trim(),
            DateOfBirth = dateOfBirth,
            PhoneNumber = phoneNumber?.Trim(),
            PhoneNumberConfirmed = phoneNumberConfirmed,
            ProfileImagePath = profileImagePath?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.AddDomainEvent(domainEvent: new Events.UserCreated(
            UserId: user.Id,
            Email: user.Email,
            UserName: user.UserName));
        return user;
    }

    #endregion

    #region Business Logic

    /// <summary>
    /// Updates various mutable properties of the user account.
    /// This method allows for partial updates; only provided parameters will be changed.
    /// </summary>
    /// <param name="email">The new email address for the user. If changed, <c>EmailConfirmed</c> is reset to false.</param>
    /// <param name="userName">The new username for the user.</param>
    /// <param name="firstName">The new first name.</param>
    /// <param name="lastName">The new last name.</param>
    /// <param name="dateOfBirth">The new date of birth.</param>
    /// <param name="profileImagePath">The new profile image path.</param>
    /// <param name="phoneNumber">The new phone number. If changed, <c>PhoneNumberConfirmed</c> is reset to false.</param>
    /// <param name="emailConfirmed">The new email confirmation status.</param>
    /// <param name="phoneNumberConfirmed">The new phone number confirmation status.</param>
    /// <returns>
    /// An <see cref="ErrorOr{User}"/> result.
    /// Returns the updated <see cref="User"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method ensures that if the email or phone number changes, their respective
    /// confirmation flags are reset to false, requiring re-confirmation.
    /// The <c>UpdatedAt</c> timestamp is updated if any changes occur, and a <see cref="Events.UserUpdated"/> domain event is added.
    /// </remarks>
    public ErrorOr<User> Update(
       string? email = null,
       string? userName = null,
       string? firstName = null,
       string? lastName = null,
       DateTimeOffset? dateOfBirth = null,
       string? profileImagePath = null,
       string? phoneNumber = null,
       bool emailConfirmed = false, // Consider removing this as it's implied by email change
       bool phoneNumberConfirmed = false) // Consider removing this as it's implied by phone change
    {
        bool changed = false;

        if (!string.IsNullOrWhiteSpace(value: email) && email.Trim() != Email)
        {
            string trimmedEmail = email.Trim();
            Email = trimmedEmail;
            NormalizedEmail = trimmedEmail.ToUpperInvariant();
            EmailConfirmed = false; // Email needs re-confirmation
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(value: userName) && userName.Trim() != UserName)
        {
            string trimmedUserName = userName.Trim();
            UserName = trimmedUserName;
            NormalizedUserName = trimmedUserName.ToUpperInvariant();
            changed = true;
        }

        if (firstName != null && firstName.Trim() != FirstName)
        {
            string trimmedFirstName = firstName.Trim();
            FirstName = trimmedFirstName;
            changed = true;
        }

        if (lastName != null && lastName.Trim() != LastName)
        {
            string trimmedLastName = lastName.Trim();
            LastName = trimmedLastName;
            changed = true;
        }

        if (dateOfBirth.HasValue && dateOfBirth != DateOfBirth)
        {
            DateOfBirth = dateOfBirth;
            changed = true;
        }

        if (profileImagePath != null && profileImagePath != ProfileImagePath)
        {
            string trimmedProfileImageUri = profileImagePath.Trim();
            ProfileImagePath = trimmedProfileImageUri;
            changed = true;
        }

        if (phoneNumber != null && phoneNumber != PhoneNumber)
        {
            string trimmedPhoneNumber = phoneNumber.Trim();
            PhoneNumber = trimmedPhoneNumber;
            PhoneNumberConfirmed = false; // Phone number needs re-confirmation
            changed = true;
        }

        if (changed)
        {
            this.MarkAsUpdated();
            AddDomainEvent(domainEvent: new Events.UserUpdated(UserId: Id));
        }

        return this;
    }

    /// <summary>
    /// Updates only the profile-related information of the user.
    /// This is a specialized update method focusing on personal details.
    /// </summary>
    /// <param name="firstName">The new first name.</param>
    /// <param name="lastName">The new last name.</param>
    /// <param name="dateOfBirth">The new date of birth.</param>
    /// <param name="profileImagePath">The new profile image path.</param>
    /// <returns>
    /// An <see cref="ErrorOr{User}"/> result.
    /// Returns the updated <see cref="User"/> instance on success.
    /// </returns>
    /// <remarks>
    /// The <c>UpdatedAt</c> timestamp is updated if any changes occur, and a <see cref="Events.UserUpdated"/> domain event is added.
    /// </remarks>
    public ErrorOr<User> UpdateProfile(
        string? firstName = null,
        string? lastName = null,
        DateTimeOffset? dateOfBirth = null,
        string? profileImagePath = null)
    {
        bool changed = false;

        if (firstName != null && firstName.Trim() != FirstName)
        {
            string trimmedFirstName = firstName.Trim();
            FirstName = trimmedFirstName;
            changed = true;
        }

        if (lastName != null && lastName.Trim() != LastName)
        {
            string trimmedLastName = lastName.Trim();
            LastName = trimmedLastName;
            changed = true;
        }

        if (dateOfBirth.HasValue && dateOfBirth != DateOfBirth)
        {
            DateOfBirth = dateOfBirth;
            changed = true;
        }

        if (profileImagePath != null && profileImagePath != ProfileImagePath)
        {
            string trimmedProfileImageUri = profileImagePath.Trim();
            ProfileImagePath = trimmedProfileImageUri;
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(domainEvent: new Events.UserUpdated(UserId: Id));
        }

        return this;
    }

    /// <summary>
    /// Updates the user's email address and resets the <c>EmailConfirmed</c> flag to false.
    /// </summary>
    /// <param name="email">The new email address for the user.</param>
    /// <returns>
    /// An <see cref="ErrorOr{User}"/> result.
    /// Returns the updated <see cref="User"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method ensures that changing the email address requires re-confirmation.
    /// An <see cref="Events.EmailChanged"/> domain event is added.
    /// </remarks>
    public ErrorOr<User> UpdateEmail(string email)
    {
        string trimmedEmail = email.Trim();
        Email = trimmedEmail;
        NormalizedEmail = trimmedEmail.ToUpperInvariant();
        EmailConfirmed = false;

        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.EmailChanged(UserId: Id,
            NewEmail: trimmedEmail));

        return this;
    }

    /// <summary>
    /// Updates the user's phone number and resets the <c>PhoneNumberConfirmed</c> flag to false.
    /// </summary>
    /// <param name="phoneNumber">The new phone number for the user.</param>
    /// <returns>
    /// An <see cref="ErrorOr{User}"/> result.
    /// Returns the updated <see cref="User"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method ensures that changing the phone number requires re-confirmation.
    /// If the phone number is not actually changed, no update occurs.
    /// An <see cref="Events.PhoneNumberChanged"/> domain event is added.
    /// </remarks>
    public ErrorOr<User> UpdatePhoneNumber(string phoneNumber)
    {
        string trimmedPhone = phoneNumber.Trim();
        if (PhoneNumber == trimmedPhone)
            return this;

        PhoneNumber = trimmedPhone;
        PhoneNumberConfirmed = false;

        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.PhoneNumberChanged(UserId: Id,
            NewPhoneNumber: trimmedPhone));

        return this;
    }

    /// <summary>
    /// Confirms the user's email address by setting the <c>EmailConfirmed</c> flag to true.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{User}"/> result.
    /// Returns the updated <see cref="User"/> instance on success.
    /// </returns>
    /// <remarks>
    /// If the email is already confirmed, no action is taken.
    /// An <see cref="Events.EmailConfirmed"/> domain event is added.
    /// </remarks>
    public ErrorOr<User> ConfirmEmail()
    {
        if (EmailConfirmed)
            return this;

        EmailConfirmed = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.EmailConfirmed(UserId: Id));

        return this;
    }

    /// <summary>
    /// Confirms the user's phone number by setting the <c>PhoneNumberConfirmed</c> flag to true.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{User}"/> result.
    /// Returns the updated <see cref="User"/> instance on success.
    /// </returns>
    /// <remarks>
    /// If the phone number is already confirmed, no action is taken.
    /// An <see cref="Events.PhoneNumberConfirmed"/> domain event is added.
    /// </remarks>
    public ErrorOr<User> ConfirmPhoneNumber()
    {
        if (PhoneNumberConfirmed)
            return this;

        PhoneNumberConfirmed = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.PhoneNumberConfirmed(UserId: Id));

        return this;
    }

    /// <summary>
    /// Records a successful sign-in attempt for the user.
    /// Updates sign-in related timestamps and increments the sign-in count.
    /// </summary>
    /// <param name="ipAddress">Optional: The IP address from which the sign-in occurred.</param>
    /// <remarks>
    /// This method updates <c>LastSignInAt</c>, <c>LastSignInIp</c>, <c>CurrentSignInAt</c>,
    /// <c>CurrentSignInIp</c>, and increments <c>SignInCount</c>.
    /// An <see cref="Events.UserSignedIn"/> domain event is added.
    /// </remarks>
    public void RecordSignIn(string? ipAddress = null)
    {

        LastSignInAt = CurrentSignInAt;
        LastSignInIp = CurrentSignInIp;
        CurrentSignInAt = DateTimeOffset.UtcNow;
        CurrentSignInIp = ipAddress ?? "Unknown";
        SignInCount++;

        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.UserSignedIn(UserId: Id,
            SignInAt: CurrentSignInAt.Value,
            IpAddress: ipAddress));
    }

    /// <summary>
    /// Locks the user's account, preventing them from signing in.
    /// </summary>
    /// <param name="lockoutEnd">Optional: The UTC timestamp when the lockout will automatically end.
    /// If null, a long-term lockout (100 years from now) is applied.</param>
    /// <returns>
    /// An <see cref="ErrorOr{User}"/> result.
    /// Returns the updated <see cref="User"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method sets <c>LockoutEnabled</c> to true and updates <c>LockoutEnd</c>.
    /// An <see cref="Events.AccountLocked"/> domain event is added.
    /// </remarks>
    public ErrorOr<User> LockAccount(DateTimeOffset? lockoutEnd = null)
    {
        LockoutEnabled = true;
        LockoutEnd = lockoutEnd ?? DateTimeOffset.UtcNow.AddYears(years: 100);

        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.AccountLocked(UserId: Id,
            LockoutEnd: LockoutEnd));

        return this;
    }

    /// <summary>
    /// Unlocks the user's account, allowing them to sign in again.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{User}"/> result.
    /// Returns the updated <see cref="User"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method clears <c>LockoutEnd</c> and resets <c>AccessFailedCount</c>.
    /// An <see cref="Events.AccountUnlocked"/> domain event is added.
    /// </remarks>
    public ErrorOr<User> UnlockAccount()
    {
        LockoutEnd = null;
        AccessFailedCount = 0;

        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.AccountUnlocked(UserId: Id));

        return this;
    }

    /// <summary>
    /// Deletes the user account from the system.
    /// This operation is subject to constraints to maintain data integrity.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Returns <see cref="Result.Deleted"/> on successful deletion.
    /// Returns <see cref="Errors.HasActiveTokens"/> if the user has active refresh tokens.
    /// Returns <see cref="Errors.HasActiveRoles"/> if the user is assigned to roles.
    /// </returns>
    /// <remarks>
    /// To prevent orphaned data and security issues, a user cannot be deleted if
    /// they have active refresh tokens or are assigned to any roles. These must be handled first.
    /// A <see cref="Events.UserDeleted"/> domain event is added upon successful deletion.
    /// </remarks>
    public ErrorOr<Deleted> Delete()
    {
        if (RefreshTokens.Any(predicate: t => !t.IsRevoked))
            return Errors.HasActiveTokens;
        if (UserRoles.Any())
            return Errors.HasActiveRoles;

        this.AddDomainEvent(domainEvent: new Events.UserDeleted(UserId: Id));
        return Result.Deleted;
    }

    /// <summary>
    /// Adds a <see cref="UserAddress"/> to the user's collection of addresses.
    /// </summary>
    /// <param name="userAddress">The <see cref="UserAddress"/> instance to add.</param>
    /// <remarks>
    /// This method directly adds the address to the <see cref="UserAddresses"/> collection.
    /// The <see cref="UserAddress"/> itself is an aggregate root, and its creation
    /// or validation would typically be handled separately.
    /// </remarks>
    public void AddAddress(UserAddress userAddress)
    {
        UserAddresses.Add(item: userAddress);
    }

    #endregion

    #region Events

    /// <summary>
    /// Defines domain events related to the lifecycle and state changes of a <see cref="User"/>.
    /// These events are crucial for enabling a decoupled, event-driven architecture, allowing
    /// other services or bounded contexts to react to user-related changes.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Raised when a new user account is created in the system.
        /// This is a base event from which more specific creation events (like registration) can derive.
        /// </summary>
        /// <param name="UserId">The unique identifier (string) of the newly created user.</param>
        /// <param name="Email">The email address of the new user.</param>
        /// <param name="UserName">The username of the new user.</param>
        public record UserCreated(string UserId, string Email, string UserName) : DomainEvent;
        /// <summary>
        /// Raised specifically when a new user registers for an account.
        /// Inherits from <see cref="UserCreated"/>.
        /// </summary>
        /// <param name="UserId">The unique identifier (string) of the registered user.</param>
        /// <param name="Email">The email address of the registered user.</param>
        /// <param name="UserName">The username of the registered user.</param>
        public sealed record UserRegistered(string UserId, string Email, string UserName) :
            UserCreated(UserId: UserId,
                Email: Email,
                UserName: UserName);

        /// <summary>
        /// Raised when an existing user's profile or account details are updated.
        /// </summary>
        /// <param name="UserId">The unique identifier (string) of the updated user.</param>
        public sealed record UserUpdated(string UserId) : DomainEvent;
        /// <summary>
        /// Raised when a user account is deleted from the system.
        /// </summary>
        /// <param name="UserId">The unique identifier (string) of the deleted user.</param>
        public sealed record UserDeleted(string UserId) : DomainEvent;
        /// <summary>
        /// Raised when a user's email address has been changed.
        /// </summary>
        /// <param name="UserId">The unique identifier (string) of the user whose email changed.</param>
        /// <param name="NewEmail">The new email address.</param>
        public sealed record EmailChanged(string UserId, string NewEmail) : DomainEvent;
        /// <summary>
        /// Raised when a user's email address has been confirmed.
        /// </summary>
        /// <param name="UserId">The unique identifier (string) of the user whose email was confirmed.</param>
        public sealed record EmailConfirmed(string UserId) : DomainEvent;
        /// <summary>
        /// Raised when a user's phone number has been changed.
        /// </summary>
        /// <param name="UserId">The unique identifier (string) of the user whose phone number changed.</param>
        /// <param name="NewPhoneNumber">The new phone number.</param>
        public sealed record PhoneNumberChanged(string UserId, string NewPhoneNumber) : DomainEvent;
        /// <summary>
        /// Raised when a user's phone number has been confirmed.
        /// </summary>
        /// <param name="UserId">The unique identifier (string) of the user whose phone number was confirmed.</param>
        public sealed record PhoneNumberConfirmed(string UserId) : DomainEvent;
        /// <summary>
        /// Raised when a user's account has been locked out.
        /// </summary>
        /// <param name="UserId">The unique identifier (string) of the user whose account was locked.</param>
        /// <param name="LockoutEnd">The UTC timestamp when the lockout is scheduled to end.</param>
        public sealed record AccountLocked(string UserId, DateTimeOffset? LockoutEnd) : DomainEvent;
        /// <summary>
        /// Raised when a user's account has been unlocked.
        /// </summary>
        /// <param name="UserId">The unique identifier (string) of the user whose account was unlocked.</param>
        public sealed record AccountUnlocked(string UserId) : DomainEvent;
        /// <summary>
        /// Raised when a user successfully signs into the application.
        /// </summary>
        /// <param name="UserId">The unique identifier (string) of the user who signed in.</param>
        /// <param name="SignInAt">The UTC timestamp of the sign-in event.</param>
        /// <param name="IpAddress">Optional: The IP address from which the sign-in occurred.</param>
        public sealed record UserSignedIn(string UserId, DateTimeOffset SignInAt, string? IpAddress) : DomainEvent;
    }
    #endregion
    #region Domain Event Helpers

    /// <summary>
    /// A private list to store domain events that have been added to this aggregate.
    /// These events are typically dispatched after the aggregate's state changes are persisted.
    /// </summary>
    private readonly List<IDomainEvent> _domainEvents = [];
    /// <summary>
    /// Gets a read-only collection of domain events associated with this aggregate.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the aggregate's internal list.
    /// </summary>
    /// <param name="domainEvent">The <see cref="IDomainEvent"/> to add.</param>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(item: domainEvent);
    }

    /// <summary>
    /// Clears all domain events from the aggregate's internal list.
    /// This method is typically called after events have been successfully dispatched.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    #endregion
}
