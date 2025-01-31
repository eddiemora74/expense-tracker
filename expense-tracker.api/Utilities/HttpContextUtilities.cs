using System.Security.Claims;

namespace expense_tracker.api.Utilities;

public static class HttpContextUtilities
{
    public static string? GetUserEmail(this HttpContext httpContext)
    {
        var user = httpContext.User;
        if (!(user.Identity?.IsAuthenticated ?? false)) return null;
        var userEmail = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userEmail ?? null;
    }
}