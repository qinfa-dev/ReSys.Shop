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
﻿/// User aggregate root - manages user identity, authentication, and profile
﻿/// Inherits from IdentityUser for ASP.NET Core Identity integration
﻿/// </summary>
public class User : IdentityUser, IHasVersion, IHasDomainEvents, IHasAuditable
{
    #region Constraints

    public static class Constraints
    {
        public static int MaxCredentialLength => Math.Max(val1: CommonInput.Constraints.NamesAndUsernames.UsernameMaxLength,
            val2: Math.Max(val1: CommonInput.Constraints.PhoneNumbers.E164MaxLength,
                val2: CommonInput.Constraints.Email.MaxLength));
        public static int MinCredentialLength => Math.Max(val1: CommonInput.Constraints.NamesAndUsernames.UsernameMinLength,
            val2: Math.Max(val1: CommonInput.Constraints.PhoneNumbers.MinLength,
                val2: CommonInput.Constraints.Email.MinLength));
    }

    #endregion

    #region Errors

    public static class Errors
    {
        public static Error NotFound(string credential) => Error.NotFound(code: $"{nameof(User)}.NotFound",
            description: $"User with credential '{credential}' was not found.");
        public static Error UserNameAlreadyExists(string userName) => Error.Conflict(
            code: $"{nameof(User)}.UserNameAlreadyExists",
            description: $"Username '{userName}' already exists.");
        public static Error EmailAlreadyExists(string email) => Error.Conflict(
            code: $"{nameof(User)}.EmailAlreadyExists",
            description: $"Email '{email}' already exists.");
        public static Error EmailNotConfirmed => Error.Validation(code: $"{nameof(User)}.EmailNotConfirmed",
            description: "Email address is not confirmed.");

        public static Error PhoneNumberAlreadyExists(string phoneNumber) => Error.Conflict(
            code: $"{nameof(User)}.PhoneNumberAlreadyExists",
            description: $"Phone number '{phoneNumber}' already exists.");

        public static Error InvalidCredentials => Error.Validation(
            code: $"{nameof(User)}.InvalidCredentials",
            description: "Invalid email or password.");
        public static Error LockedOut => Error.Validation(code: $"{nameof(User)}.LockedOut",
            description: "Account is locked out.");

        public static Error InvalidToken => Error.Validation(code: $"{nameof(User)}.InvalidToken",
            description: "Invalid or expired token.");

        public static Error HasActiveTokens => Error.Validation(code: $"{nameof(User)}.HasActiveTokens",
            description: "Cannot delete user with active refresh tokens.");
        public static Error HasActiveRoles => Error.Validation(code: $"{nameof(User)}.HasActiveRoles",
            description: "Cannot delete user with assigned roles.");

        public static Error Unauthorized =>
            Error.Unauthorized(code: $"{nameof(User)}.Unauthorized",
                description: "User is not authorized to access this resource.");
    }

    #endregion

    #region Properties

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }
    public string? ProfileImagePath { get; set; }

    public DateTimeOffset? LastSignInAt { get; set; }
    public string? LastSignInIp { get; set; }
    public DateTimeOffset? CurrentSignInAt { get; set; }
    public string? CurrentSignInIp { get; set; }
    public int SignInCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public long Version { get; set; }

    #endregion

    #region Relationships

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<UserClaim> Claims { get; set; } = [];
    public ICollection<UserLogin> UserLogins { get; set; } = [];
    public ICollection<UserToken> UserTokens { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<UserAddress> UserAddresses { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = new List<Order>(); 
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<PaymentSource> PaymentSources { get; set; } = new List<PaymentSource>();

    #endregion

    #region Computed Properties

    public string FullName => $"{FirstName} {LastName}".Trim();
    public bool HasProfile => !string.IsNullOrWhiteSpace(value: FirstName) || !string.IsNullOrWhiteSpace(value: LastName);
    public bool IsActive => !LockoutEnabled || LockoutEnd == null || LockoutEnd <= DateTimeOffset.UtcNow;
    public UserAddress? DefaultBillingAddress => UserAddresses.FirstOrDefault(predicate: ua => ua is { IsDefault: true, Type: AddressType.Billing });
    public UserAddress? DefaultShippingAddress => UserAddresses.FirstOrDefault(predicate: ua => ua is { IsDefault: true, Type: AddressType.Shipping });
    #endregion

    #region Factory Methods

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
            Email = trimmedEmail,
            NormalizedEmail = trimmedEmail.ToUpperInvariant(),
            EmailConfirmed = emailConfirmed,
            UserName = effectiveUserName,
            NormalizedUserName = effectiveUserName.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(format: "N"),
            ConcurrencyStamp = Guid.NewGuid().ToString(format: "N"),
            LockoutEnabled = true,
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

    public ErrorOr<User> Update(
       string? email = null,
       string? userName = null,
       string? firstName = null,
       string? lastName = null,
       DateTimeOffset? dateOfBirth = null,
       string? profileImagePath = null,
       string? phoneNumber = null,
       bool emailConfirmed = false,
       bool phoneNumberConfirmed = false)
    {
        bool changed = false;

        if (!string.IsNullOrWhiteSpace(value: email) && email.Trim() != Email)
        {
            string trimmedEmail = email.Trim();
            Email = trimmedEmail;
            NormalizedEmail = trimmedEmail.ToUpperInvariant();
            EmailConfirmed = false;
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
            PhoneNumberConfirmed = false;
            changed = true;
        }

        if (changed)
        {
            this.MarkAsUpdated();
            AddDomainEvent(domainEvent: new Events.UserUpdated(UserId: Id));
        }

        return this;
    }

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

    public ErrorOr<User> ConfirmEmail()
    {
        if (EmailConfirmed)
            return this;

        EmailConfirmed = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.EmailConfirmed(UserId: Id));

        return this;
    }

    public ErrorOr<User> ConfirmPhoneNumber()
    {
        if (PhoneNumberConfirmed)
            return this;

        PhoneNumberConfirmed = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.PhoneNumberConfirmed(UserId: Id));

        return this;
    }

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

    public ErrorOr<User> LockAccount(DateTimeOffset? lockoutEnd = null)
    {
        LockoutEnabled = true;
        LockoutEnd = lockoutEnd ?? DateTimeOffset.UtcNow.AddYears(years: 100);

        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.AccountLocked(UserId: Id,
            LockoutEnd: LockoutEnd));

        return this;
    }

    public ErrorOr<User> UnlockAccount()
    {
        LockoutEnd = null;
        AccessFailedCount = 0;

        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.AccountUnlocked(UserId: Id));

        return this;
    }

    public ErrorOr<Deleted> Delete()
    {
        if (RefreshTokens.Any(predicate: t => !t.IsRevoked))
            return Errors.HasActiveTokens;
        if (UserRoles.Any())
            return Errors.HasActiveRoles;

        this.AddDomainEvent(domainEvent: new Events.UserDeleted(UserId: Id));
        return Result.Deleted;
    }

    public void AddAddress(UserAddress userAddress)
    {
        UserAddresses.Add(item: userAddress);
    }

    #endregion

    #region Events

    public static class Events
    {

        public record UserCreated(string UserId, string Email, string UserName) : DomainEvent;
        public sealed record UserRegistered(string UserId, string Email, string UserName) :
            UserCreated(UserId: UserId,
                Email: Email,
                UserName: UserName);

        public sealed record UserUpdated(string UserId) : DomainEvent;
        public sealed record UserDeleted(string UserId) : DomainEvent;
        public sealed record EmailChanged(string UserId, string NewEmail) : DomainEvent;
        public sealed record EmailConfirmed(string UserId) : DomainEvent;
        public sealed record PhoneNumberChanged(string UserId, string NewPhoneNumber) : DomainEvent;
        public sealed record PhoneNumberConfirmed(string UserId) : DomainEvent;
        public sealed record AccountLocked(string UserId, DateTimeOffset? LockoutEnd) : DomainEvent;
        public sealed record AccountUnlocked(string UserId) : DomainEvent;
        public sealed record UserSignedIn(string UserId, DateTimeOffset SignInAt, string? IpAddress) : DomainEvent;
    }
    #endregion
    #region Domain Event Helpers

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(item: domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    #endregion
}
