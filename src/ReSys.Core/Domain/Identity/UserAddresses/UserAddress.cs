using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Domain.Location;
using ReSys.Core.Domain.Location.Countries;
using ReSys.Core.Domain.Location.States;

namespace ReSys.Core.Domain.Identity.UserAddresses;

public enum AddressType
{
    Shipping,
    Billing
}

public sealed class UserAddress : Aggregate<Guid>, IAddress
{
    #region Constraints

    public static class UserAddressConstraints
    {
        public const int LabelMaxLength = CommonInput.Constraints.NamesAndUsernames.NameMaxLength;
    }

    #endregion

    #region Errors

    public static class Errors
    {
        public static Error NotFound(Guid id) => Error.NotFound(code: "UserAddress.NotFound",
            description: $"UserAddress with ID '{id}' was not found.");
    }

    #endregion

    #region Properties

    public string? FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool QuickCheckout { get; set; }
    public bool IsDefault { get; set; }
    public AddressType Type { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? Zipcode { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }

    #endregion

    #region Relationships

    public string UserId { get; set; } = null!;
    public ApplicationUser ApplicationUser { get; set; } = null!;

    public Guid CountryId { get; set; }
    public Country? Country { get; set; } = null!;

    public Guid? StateId { get; set; }
    public State? State { get; set; } = null!;

    #endregion

    #region Constructors

    private UserAddress() { }

    #endregion

    #region Factory Methods

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

    public ErrorOr<Deleted> Delete()
    {
        AddDomainEvent(domainEvent: new Events.UserAddressDeleted(UserAddressId: Id));
        return Result.Deleted;
    }

    #endregion


    #region Events

    public static class Events
    {
        /// <summary>
        /// Domain event raised when a new user address is created.
        /// Purpose: Notifies other parts of the system (e.g., user profile management, order processing) that a new address has been associated with a user.
        /// </summary>
        public sealed record UserAddressCreated(Guid UserAddressId) : DomainEvent;

        /// <summary>
        /// Domain event raised when an existing user address is updated.
        /// Purpose: Signals that a user's address details have changed, prompting dependent services to re-evaluate or update their records.
        /// </summary>
        public sealed record UserAddressUpdated(Guid UserAddressId) : DomainEvent;

        /// <summary>
        /// Domain event raised when a user address is deleted.
        /// Purpose: Indicates a user address has been removed, requiring cleanup, invalidation of references, or logging of the deletion in related services.
        /// </summary>
        public sealed record UserAddressDeleted(Guid UserAddressId) : DomainEvent;
    }

    #endregion
}