using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Domain.Location;
using ReSys.Core.Domain.Location.Countries;
using ReSys.Core.Domain.Location.States;

namespace ReSys.Core.Domain.Identity.UserAddresses;

/// <summary>
/// Defines the type of an address, indicating its primary use case.
/// </summary>
public enum AddressType
{
    /// <summary>
    /// The address is primarily used for shipping physical goods to the user.
    /// </summary>
    Shipping,
    /// <summary>
    /// The address is primarily used for billing purposes, such as credit card statements or invoices.
    /// </summary>
    Billing
}

/// <summary>
/// Represents a user's physical address, which can be used for shipping, billing, or both.
/// This entity extends the generic <see cref="IAddress"/> interface and is part of a user's aggregate.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Identity Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>User Profile</term>
/// <description>Stores multiple addresses for a user (e.g., home, work, vacation).</description>
/// </item>
/// <item>
/// <term>E-commerce Operations</term>
/// <description>Essential for order fulfillment (shipping) and payment processing (billing).</description>
/// </item>
/// <item>
/// <term>Address Management</term>
/// <description>Allows users to mark default addresses and manage quick checkout preferences.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes:</strong>
/// <list type="bullet">
/// <item>
/// <term>Label</term>
/// <description>A user-defined name for the address (e.g., "Home", "Work").</description>
/// </item>
/// <item>
/// <term>IsDefault</term>
/// <description>Indicates if this is the user's preferred default address for its <see cref="Type"/>.</description>
/// </item>
/// <item>
/// <term>QuickCheckout</term>
/// <description>Flag for expedited checkout processes.</description>
/// </item>
/// <item>
/// <term>AddressType</term>
/// <description>Distinguishes between Shipping and Billing addresses.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Concerns Implemented:</strong>
/// <list type="bullet">
/// <item><strong>IAddress</strong> - Ensures standard address properties.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class UserAddress : Aggregate<Guid>, IAddress
{
    #region Constraints

    /// <summary>
    /// Defines constraints and constant values specific to <see cref="UserAddress"/> properties.
    /// </summary>
    public static class UserAddressConstraints
    {
        /// <summary>Maximum allowed length for an address label.</summary>
        public const int LabelMaxLength = CommonInput.Constraints.NamesAndUsernames.NameMaxLength;
    }

    #endregion

    #region Errors

    /// <summary>
    /// Defines domain error scenarios specific to <see cref="UserAddress"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a requested <see cref="UserAddress"/> could not be found.
        /// </summary>
        /// <param name="id">The unique identifier of the user address that was not found.</param>
        public static Error NotFound(Guid id) => Error.NotFound(code: "UserAddress.NotFound",
            description: $"UserAddress with ID '{id}' was not found.");
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the first name associated with this address.
    /// </summary>
    public string? FirstName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the last name associated with this address.
    /// </summary>
    public string? LastName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets an optional user-defined label for the address (e.g., "Home", "Work").
    /// </summary>
    public string? Label { get; set; }
    /// <summary>
    /// Gets or sets a flag indicating if this address is preferred for quick checkout.
    /// </summary>
    public bool QuickCheckout { get; set; }
    /// <summary>
    /// Gets or sets a flag indicating if this is the default address for its <see cref="Type"/>.
    /// </summary>
    public bool IsDefault { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="AddressType"/> (e.g., Shipping, Billing).
    /// </summary>
    public AddressType Type { get; set; }
    /// <summary>
    /// Gets or sets the first line of the street address.
    /// </summary>
    public string? Address1 { get; set; }
    /// <summary>
    /// Gets or sets the second line of the street address (optional).
    /// </summary>
    public string? Address2 { get; set; }
    /// <summary>
    /// Gets or sets the city or town name.
    /// </summary>
    public string? City { get; set; }
    /// <summary>
    /// Gets or sets the postal code or ZIP code.
    /// </summary>
    public string? Zipcode { get; set; }
    /// <summary>
    /// Gets or sets the phone number associated with this address.
    /// </summary>
    public string? Phone { get; set; }
    /// <summary>
    /// Gets or sets the company name associated with this address (optional).
    /// </summary>
    public string? Company { get; set; }

    #endregion

    #region Relationships

    /// <summary>
    /// Gets or sets the unique identifier of the <see cref="User"/> who owns this address.
    /// </summary>
    public string UserId { get; set; } = null!;
    /// <summary>
    /// Gets or sets the navigation property to the <see cref="User"/> who owns this address.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the <see cref="Country"/> associated with this address.
    /// </summary>
    public Guid CountryId { get; set; }
    /// <summary>
    /// Gets or sets the navigation property to the <see cref="Country"/> associated with this address.
    /// </summary>
    public Country? Country { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the <see cref="State"/> associated with this address (optional).
    /// </summary>
    public Guid? StateId { get; set; }
    /// <summary>
    /// Gets or sets the navigation property to the <see cref="State"/> associated with this address (optional).
    /// </summary>
    public State? State { get; set; } = null!;

    #endregion

    #region Constructors

    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private UserAddress() { }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Factory method to create a new <see cref="UserAddress"/> instance.
    /// Performs basic trimming and initializes creation timestamp.
    /// </summary>
    /// <param name="firstName">The first name associated with the address.</param>
    /// <param name="lastName">The last name associated with the address.</param>
    /// <param name="userId">The ID of the <see cref="User"/> who owns this address.</param>
    /// <param name="countryId">The ID of the <see cref="Country"/> for this address.</param>
    /// <param name="address1">The first line of the street address.</param>
    /// <param name="city">The city or town name.</param>
    /// <param name="zipcode">The postal code or ZIP code.</param>
    /// <param name="stateId">Optional: The ID of the <see cref="State"/> for this address.</param>
    /// <param name="address2">Optional: The second line of the street address.</param>
    /// <param name="phone">Optional: The phone number for this address.</param>
    /// <param name="company">Optional: The company name for this address.</param>
    /// <param name="label">Optional: A user-defined label for this address (e.g., "Home").</param>
    /// <param name="quickCheckout">Flag indicating if this address is preferred for quick checkout. Defaults to false.</param>
    /// <param name="isDefault">Flag indicating if this is the default address for its <see cref="AddressType"/>. Defaults to false.</param>
    /// <param name="type">The <see cref="AddressType"/> (Shipping or Billing). Defaults to Shipping.</param>
    /// <returns>
    /// An <see cref="ErrorOr{UserAddress}"/> result.
    /// Returns the newly created <see cref="UserAddress"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method adds a <see cref="Events.UserAddressCreated"/> domain event.
    /// Further validation (e.g., required fields, format validation) is typically handled by FluentValidation.
    /// </remarks>
    public static ErrorOr<UserAddress> Create(
        string firstName,
        string lastName,
        string userId,
        Guid countryId,
        string address1,
        string city,
        string zipcode,
        Guid? stateId = null,
        string? address2 = null,
        string? phone = null,
        string? company = null,
        string? label = null,
        bool quickCheckout = false,
        bool isDefault = false,
        AddressType type = AddressType.Shipping)
    {
        UserAddress userAddress = new()
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Label = label?.Trim(),
            QuickCheckout = quickCheckout,
            IsDefault = isDefault,
            Type = type,
            UserId = userId,
            CountryId = countryId,
            StateId = stateId,
            Address1 = address1.Trim(),
            Address2 = address2?.Trim(),
            City = city.Trim(),
            Zipcode = zipcode.Trim(),
            Phone = phone?.Trim(),
            Company = company?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        userAddress.AddDomainEvent(domainEvent: new Events.UserAddressCreated(UserAddressId: userAddress.Id));
        return userAddress;
    }

    #endregion

    #region Business Logic

    /// <summary>
    /// Updates the mutable properties of the <see cref="UserAddress"/>.
    /// This method allows for partial updates; only provided parameters will be changed.
    /// </summary>
    /// <param name="firstName">The new first name for the address. If null, the existing first name is retained.</param>
    /// <param name="lastName">The new last name for the address. If null, the existing last name is retained.</param>
    /// <param name="label">The new user-defined label. If null, the existing label is retained.</param>
    /// <param name="quickCheckout">The new quick checkout flag. If null, the existing flag is retained.</param>
    /// <param name="isDefault">The new default address flag. If null, the existing flag is retained.</param>
    /// <param name="type">The new <see cref="AddressType"/>. If null, the existing type is retained.</param>
    /// <param name="address1">The new first line of the street address. If null, the existing address is retained.</param>
    /// <param name="address2">The new second line of the street address. If null, the existing address is retained.</param>
    /// <param name="city">The new city. If null, the existing city is retained.</param>
    /// <param name="zipcode">The new postal code. If null, the existing postal code is retained.</param>
    /// <param name="countryId">The new <see cref="Country"/> ID. If null, the existing country ID is retained.</param>
    /// <param name="stateId">The new <see cref="State"/> ID. If null, the existing state ID is retained.</param>
    /// <param name="phone">The new phone number. If null, the existing phone number is retained.</param>
    /// <param name="company">The new company name. If null, the existing company name is retained.</param>
    /// <returns>
    /// An <see cref="ErrorOr{UserAddress}"/> result.
    /// Returns the updated <see cref="UserAddress"/> instance on success.
    /// </returns>
    /// <remarks>
    /// If any changes occur, the <c>UpdatedAt</c> timestamp is updated, and a <see cref="Events.UserAddressUpdated"/> domain event is added.
    /// Further validation (e.g., required fields, format validation) is typically handled by FluentValidation.
    /// </remarks>
    public ErrorOr<UserAddress> Update(
        string? firstName = null,
        string? lastName = null,
        string? label = null,
        bool? quickCheckout = null,
        bool? isDefault = null,
        AddressType? type = null,
        string? address1 = null,
        string? address2 = null,
        string? city = null,
        string? zipcode = null,
        Guid? countryId = null,
        Guid? stateId = null,
        string? phone = null,
        string? company = null)
    {
        bool changed = false;

        if (firstName != null && firstName != FirstName)
        {
            FirstName = firstName.Trim();
            changed = true;
        }

        if (lastName != null && lastName != LastName)
        {
            LastName = lastName.Trim();
            changed = true;
        }

        if (label != null && label != Label)
        {
            Label = label.Trim();
            changed = true;
        }

        if (quickCheckout.HasValue && quickCheckout != QuickCheckout)
        {
            QuickCheckout = quickCheckout.Value;
            changed = true;
        }

        if (isDefault.HasValue && isDefault != IsDefault)
        {
            IsDefault = isDefault.Value;
            changed = true;
        }

        if (type.HasValue && type != Type)
        {
            Type = type.Value;
            changed = true;
        }

        if (address1 != null && Address1 != address1)
        {
            Address1 = address1.Trim();
            changed = true;
        }

        if (address2 != null && Address2 != address2)
        {
            Address2 = address2.Trim();
            changed = true;
        }

        if (city != null && City != city)
        {
            City = city.Trim();
            changed = true;
        }

        if (zipcode != null && Zipcode != zipcode)
        {
            Zipcode = zipcode.Trim();
            changed = true;
        }

        if (countryId.HasValue && countryId != CountryId)
        {
            CountryId = countryId.Value;
            changed = true;
        }

        if (stateId != null && stateId != StateId)
        {
            StateId = stateId.Value;
            changed = true;
        }

        if (phone != null && Phone != phone)
        {
            Phone = phone.Trim();
            changed = true;
        }

        if (company != null && Company != company)
        {
            Company = company.Trim();
            changed = true;
        }


        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(domainEvent: new Events.UserAddressUpdated(UserAddressId: Id));
        }

        return this;
    }

    /// <summary>
    /// Deletes the <see cref="UserAddress"/> from the system.
    /// This method adds a <see cref="Events.UserAddressDeleted"/> domain event.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Always returns <see cref="Result.Deleted"/> on success.
    /// </returns>
    /// <remarks>
    /// The actual removal from persistence is handled by the application service layer
    /// in response to the domain event.
    /// </remarks>
    public ErrorOr<Deleted> Delete()
    {
        AddDomainEvent(domainEvent: new Events.UserAddressDeleted(UserAddressId: Id));
        return Result.Deleted;
    }

    #endregion


    #region Events

    /// <summary>
    /// Defines domain events related to the lifecycle and state changes of a <see cref="UserAddress"/>.
    /// These events are crucial for enabling a decoupled, event-driven architecture, allowing
    /// other services or bounded contexts to react to user address-related changes.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Domain event raised when a new user address is created.
        /// Purpose: Notifies other parts of the system (e.g., user profile management, order processing) that a new address has been associated with a user.
        /// </summary>
        /// <param name="UserAddressId">The unique identifier of the newly created user address.</param>
        public sealed record UserAddressCreated(Guid UserAddressId) : DomainEvent;

        /// <summary>
        /// Domain event raised when an existing user address is updated.
        /// Purpose: Signals that a user's address details have changed, prompting dependent services to re-evaluate or update their records.
        /// </summary>
        /// <param name="UserAddressId">The unique identifier of the updated user address.</param>
        public sealed record UserAddressUpdated(Guid UserAddressId) : DomainEvent;

        /// <summary>
        /// Domain event raised when a user address is deleted.
        /// Purpose: Indicates a user address has been removed, requiring cleanup, invalidation of references, or logging of the deletion in related services.
        /// </summary>
        /// <param name="UserAddressId">The unique identifier of the deleted user address.</param>
        public sealed record UserAddressDeleted(Guid UserAddressId) : DomainEvent;
    }

    #endregion
}