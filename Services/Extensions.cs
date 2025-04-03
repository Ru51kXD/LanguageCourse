using System.Security.Claims;

namespace WebApplication15.Services
{
    public static class Extensions
    {
        public static bool IsAuthenticated(this ClaimsPrincipal user)
        {
            return user?.Identity?.IsAuthenticated ?? false;
        }

        public static bool IsInRole(this ClaimsPrincipal user, string role)
        {
            return user?.IsInRole(role) ?? false;
        }
    }
} 