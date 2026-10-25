using System.Collections.Generic;

namespace customerseller.Helpers
{
    public static class ChatPermissions
    {
        private static readonly Dictionary<string, List<string>> Allowed =
            new Dictionary<string, List<string>>
            {
                { "Customer",  new List<string> { "Seller", "Admin" } },
                { "Seller",    new List<string> { "Customer", "Admin", "Rider" } },
                { "Rider",   new List<string> { "Admin", "Seller" } },
                { "Moderator", new List<string> { "Admin" } },
                { "Admin",     new List<string> { "Seller", "Rider", "Moderator", "Customer" } }
            };

        public static List<string> GetAllowedRolesFor(string role)
        {
            if (role != null && Allowed.TryGetValue(role, out var list))
                return list;
            return new List<string>();
        }

        public static bool IsAllowed(string role, string otherRole)
        {
            if (role == null || otherRole == null) return false;

            return GetAllowedRolesFor(role).Contains(otherRole)
                && GetAllowedRolesFor(otherRole).Contains(role);
        }
    }
}