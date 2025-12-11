namespace ReSys.Core.Domain.Location;

public interface IAddress
{
    #region Properties
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? Zipcode { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }

    #endregion
}

#region Constraints

public static class AddressConstraints
{
    public const int Address1MaxLength = CommonInput.Constraints.Text.ShortTextMaxLength;
    public const int Address2MaxLength = CommonInput.Constraints.Text.ShortTextMaxLength;
    public const int CityMaxLength = CommonInput.Constraints.NamesAndUsernames.NameMaxLength;

    public const int
        ZipcodeMaxLength = 20; // Keeping 20 for flexibility,CommonInput.Constraints.ZipCodeMaxLength is 10.

    public const int PhoneMaxLength = 50; // Keeping 50 for flexibility,CommonInput.Constraints.PhoneMaxLength is 20.
    public const int CompanyMaxLength = CommonInput.Constraints.NamesAndUsernames.NameMaxLength;
}

#endregion
