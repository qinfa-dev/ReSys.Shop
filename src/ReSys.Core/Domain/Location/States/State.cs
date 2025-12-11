using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Identity.UserAddresses;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Location.Countries;
using ReSys.Core.Domain.Stores;

namespace ReSys.Core.Domain.Location.States;

public sealed class State : AuditableEntity<Guid>
{
    #region Errors
    public static class Errors
    {
        public static Error NotFound(Guid id) => Error.NotFound(code: "State.NotFound",
            description: $"State with ID '{id}' was not found.");
        public static Error CannotDeleteWithAddresses => Error.Conflict(code: "State.CannotDeleteWithAddresses",
            description: "Cannot delete state with associated addresses.");
    }
    #endregion

    #region Properties
    public string Name { get; set; } = string.Empty;
    public string? Abbr { get; set; }
    public Guid CountryId { get; set; }
    #endregion

    #region Relationships
    public Country Country { get; set; } = null!;
    public ICollection<Store> Stores { get; set; } = new List<Store>();
    public ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();
    public ICollection<StockLocation> StockLocations { get; set; } = new List<StockLocation>();
    #endregion

    #region Constructors
    private State() { }
    #endregion

    #region Factory Methods
    public static ErrorOr<State> Create(string name, string? abbr, Guid countryId)
    {
        State state = new()
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Abbr = abbr?.Trim().ToUpper(),
            CountryId = countryId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return state;
    }
    #endregion

    #region Business Logic
    public ErrorOr<State> Update(string? name = null, string? abbr = null, Guid? countryId = null)
    {
        bool changed = false;

        if (name != null && Name != name)
        {
            Name = name.Trim();
            changed = true;
        }

        if (abbr != null && Abbr != abbr)
        {
            Abbr = abbr.Trim().ToUpper();
            changed = true;
        }

        if (countryId.HasValue && countryId != CountryId)
        {
            CountryId = countryId.Value;
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        return this;
    }

    public ErrorOr<Deleted> Delete()
    {
        if (UserAddresses.Any())
            return Errors.CannotDeleteWithAddresses;

        return Result.Deleted;
    }
    #endregion

}