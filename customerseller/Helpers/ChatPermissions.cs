using System.Collections.Generic;

namespace customerseller.Helpers
{
    // Decide karta hai ke kaun kis role se chat kar sakta hai.
    // Confirmed pairs: Customer-Seller, Seller-Admin, Admin-Moderator, Seller-Moderator
    public static class ChatPermissions
    {
        private static readonly HashSet<(string, string)> AllowedPairs = new()
{
    ("Customer", "Seller"), ("Seller", "Customer"),
    ("Seller", "Admin"),    ("Admin", "Seller"),
    ("Admin", "Moderator"), ("Moderator", "Admin"),
    ("Seller", "Moderator"),("Moderator", "Seller"),
    ("Courier", "Admin"),   ("Admin", "Courier"),
};

        public static bool IsAllowed(string role1, string role2)
        {
            if (string.IsNullOrWhiteSpace(role1) || string.IsNullOrWhiteSpace(role2))
                return false;

            return AllowedPairs.Contains((role1, role2));
        }

        // Diye gaye role ke liye, wo saare roles wapas karta hai jinse chat ho sakti hai
        public static List<string> GetAllowedRolesFor(string role)
        {
            var result = new List<string>();
            foreach (var pair in AllowedPairs)
            {
                if (pair.Item1 == role)
                    result.Add(pair.Item2);
            }
            return result;
        }
    }
}
