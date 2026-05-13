using System.Security.Claims;

public static class UserRoles
{
    public const string Admin = "Admin";

    public const string User = "User";

    public const string Moderator = "Moderator";

    public const string Guest = "Guest";

    public static string GetUserRole(ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Role)?.Value ?? "No role";
}