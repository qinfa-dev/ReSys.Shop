using Microsoft.AspNetCore.Identity;

namespace ReSys.Core.Feature.Accounts.Common;
public static partial class Account
{
    public static List<Error> ToApplicationResult(
        this IEnumerable<IdentityError> errors,
        string prefix = "Auth",
        string fallbackCode = "UnknownError",
        ErrorType errorType = ErrorType.Validation)
    {
        return [.. errors.Select(selector: error => Error.Custom(
            type: (int)errorType,
            code: !string.IsNullOrWhiteSpace(value: error.Code) ? $"{prefix}.{error.Code}" : $"{prefix}.{fallbackCode}",
            description: error.Description))];
    }
}
