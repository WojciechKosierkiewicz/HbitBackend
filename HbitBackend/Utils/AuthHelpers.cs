using System.Security.Claims;

namespace HbitBackend.Utils;

internal static class AuthHelpers
{
    internal static bool TryGetUserId(ClaimsPrincipal? user, out int userId)
    {
        userId = 0;
        if (user == null) return false;

        var claimValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claimValue)) return false;

        return int.TryParse(claimValue, out userId);
    }
}
