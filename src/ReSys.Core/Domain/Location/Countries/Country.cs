using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Identity.UserAddresses;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Location.States;
using ReSys.Core.Domain.Stores;

namespace ReSys.Core.Domain.Location.Countries;

public sealed class Country : Aggregate
{
    #region Constraints
    public static class Constraints
    {
        public const int IsoMaxLength = 2;
        public const int Iso3MaxLength = 3;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error NotFound(Guid id) => Error.NotFound(code: "Country.NotFound",
            description: $"Address with ID '{id}' was not found.");
        public static Error CannotDeleteWithDependencies => Error.Conflict(code: "Country.CannotDeleteWithDependencies",
            description: "Cannot delete country with associated addresses or states.");
    }
    #endregion

    #region Properties
    public string Name { get; set; } = string.Empty;
    public string Iso { get; set; } = string.Empty;
    public string Iso3 { get; set; } = string.Empty;
    #endregion

    #region Relationships
    public ICollection<State> States { get; set; } = new List<State>();
    public ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();
    public ICollection<StockLocation> StockLocations { get; set; } = new List<StockLocation>();
    public ICollection<Store> Stores { get; set; } = new List<Store>();
    #endregion

    #region Constructors
    private Country() { }
    #endregion

    #region Factory Methods
    public static ErrorOr<Country> Create(string name, string iso, string iso3)
    {
        Country country = new()
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Iso = iso.Trim().ToUpper(),
            Iso3 = iso3.Trim().ToUpper(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        country.AddDomainEvent(domainEvent: new Events.CountryCreated(CountryId: country.Id));
        return country;
    }
    #endregion

    #region Business Logic
    public ErrorOr<Country> Update(string? name = null, string? iso = null, string? iso3 = null)
    {
        bool changed = false;

        if (name != null && Name != name)
        {
            Name = name.Trim();
            changed = true;
        }

        if (iso != null && Iso != iso)
        {
            Iso = iso.Trim().ToUpper();
            changed = true;
        }

        if (iso3 != null && Iso3 != iso3)
        {
            Iso3 = iso3.Trim().ToUpper();
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(domainEvent: new Events.CountryUpdated(CountryId: Id));
        }

        return this;
    }

    public ErrorOr<Deleted> Delete()
    {
        if (UserAddresses.Any() || StockLocations.Any() || States.Any())
            return Errors.CannotDeleteWithDependencies;

        AddDomainEvent(domainEvent: new Events.CountryDeleted(CountryId: Id));
        return Result.Deleted;
    }
    #endregion

    #region Events
    public static class Events
    {
        /// <summary>
        /// Domain event raised when a new country is created.
        /// Purpose: Notifies the system that a new country is available, potentially impacting shipping regions, tax configurations, or localization settings.
        /// </summary>
        public sealed record CountryCreated(Guid CountryId) : DomainEvent;

        /// <summary>
        /// Domain event raised when an existing country is updated.
        /// Purpose: Informs the system that country details have been updated, which may require adjustments in configurations, data processing, or regional settings.
        /// </summary>
        public sealed record CountryUpdated(Guid CountryId) : DomainEvent;

        /// <summary>
        /// Domain event raised when a country is deleted.
        /// Purpose: Signals the removal of a country, necessitating cleanup, data migration, or invalidation of related entities (e.g., states, addresses).
        /// </summary>
        public sealed record CountryDeleted(Guid CountryId) : DomainEvent;
    }
    #endregion

}