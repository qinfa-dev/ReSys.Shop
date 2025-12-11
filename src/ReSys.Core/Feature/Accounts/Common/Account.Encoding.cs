using System.Text;

using Microsoft.AspNetCore.WebUtilities;

using ReSys.Core.Domain.Identity.Users;

using Serilog;

namespace ReSys.Core.Feature.Accounts.Common;
public static partial class Account
{
    public static ErrorOr<string> DecodeToken(this string code)
    {
        try
        {
            return Encoding.UTF8.GetString(bytes: WebEncoders.Base64UrlDecode(input: code));
        }
        catch (FormatException ex)
        {
            Log.Error(exception: ex,
                messageTemplate: "Failed to decode token");
            return Error.Unexpected(code: $"{nameof(User)}.DecodeTokenFailed");
        }
    }
}
