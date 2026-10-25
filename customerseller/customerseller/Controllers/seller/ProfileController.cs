using customerseller.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace customerseller.Controllers.seller
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Profile()
        {
            var email = HttpContext.Session.GetString("UserEmail") ?? "";

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();

           
            var userCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT FirstName, LastName, Email, Phone FROM Users WHERE Email = @email", con);
            userCmd.Parameters.AddWithValue("@email", email);
            var userReader = userCmd.ExecuteReader();

            string fullName = "", phone = "";
            if (userReader.Read())
            {
                fullName = userReader["FirstName"]?.ToString() + " " + userReader["LastName"]?.ToString();
                phone = userReader["Phone"]?.ToString() ?? "";
            }
            userReader.Close();


            var shopCmd = new Microsoft.Data.SqlClient.SqlCommand(
      "SELECT ShopName, CNIC, SubCategories, SellerPhone, SellerArea, OwnerName FROM SellerShops WHERE SellerEmail = @email", con);
            shopCmd.Parameters.AddWithValue("@email", email);
            var shopReader = shopCmd.ExecuteReader();

            string shopName = "", cnic = "", categories = "", sellerPhone = "", sellerArea = "", ownerName = "";
            if (shopReader.Read())
            {
                shopName = shopReader["ShopName"]?.ToString() ?? "";
                cnic = shopReader["CNIC"]?.ToString() ?? "";
                categories = shopReader["SubCategories"]?.ToString() ?? "";
                sellerPhone = shopReader["SellerPhone"]?.ToString() ?? "";
                sellerArea = shopReader["SellerArea"]?.ToString() ?? "";
                ownerName = shopReader["OwnerName"]?.ToString() ?? "";
            }
            shopReader.Close();
            var profile = new UserProfile
            {
                FullName = !string.IsNullOrWhiteSpace(ownerName) ? ownerName.Trim() : fullName.Trim(),
                ShopName = shopName,
                Email = email,
                Phone = string.IsNullOrEmpty(phone) ? sellerPhone : phone,
                CNIC = cnic,
                Categories = categories,
                Area = sellerArea
            };
            return View("~/Views/Seller/Profile/Profile.cshtml", profile);
        }
        [HttpPost]
        public IActionResult UpdateProfile(UserProfile model)
        {
            var email = HttpContext.Session.GetString("UserEmail") ?? "";

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();

            var userCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "UPDATE Users SET Phone = @phone WHERE Email = @email", con);
            userCmd.Parameters.AddWithValue("@phone", model.Phone ?? "");
            userCmd.Parameters.AddWithValue("@email", email);
            userCmd.ExecuteNonQuery();

            string oldShopName = "", oldCategories = "", oldOwnerName = "";
            var oldCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT ShopName, SubCategories, OwnerName FROM SellerShops WHERE SellerEmail = @email", con);
            oldCmd.Parameters.AddWithValue("@email", email);
            var oldReader = oldCmd.ExecuteReader();
            if (oldReader.Read())
            {
                oldShopName = oldReader["ShopName"]?.ToString() ?? "";
                oldCategories = oldReader["SubCategories"]?.ToString() ?? "";
                oldOwnerName = oldReader["OwnerName"]?.ToString() ?? "";
            }
            oldReader.Close();

            var changes = new List<string>();

            if (!string.Equals(oldShopName.Trim(), (model.ShopName ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
            {
                changes.Add($"Shop name changed from \"{oldShopName}\" to \"{model.ShopName}\"");
            }

            if (!string.Equals(oldOwnerName.Trim(), (model.FullName ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
            {
                changes.Add($"Owner name changed from \"{oldOwnerName}\" to \"{model.FullName}\"");
            }

            var oldCatList = oldCategories.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToList();
            var newCatList = (model.Categories ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToList();

            var addedCats = newCatList.Except(oldCatList).ToList();
            var removedCats = oldCatList.Except(newCatList).ToList();

            if (addedCats.Any())
                changes.Add("Added categories: " + string.Join(", ", addedCats));
            foreach (var newCat in addedCats)
            {
                bool alreadyExists = _context.SubCategoryVideos
                    .Any(v => v.SellerEmail == email && v.SubCategory == newCat);

                if (!alreadyExists)
                {
                    _context.SubCategoryVideos.Add(new SubCategoryVideo
                    {
                        SellerEmail = email,
                        SubCategory = newCat,
                        VideoStatus = "Pending",
                        VideoUrl = null
                    });
                }
            }
            _context.SaveChanges();
            if (removedCats.Any())
                changes.Add("Removed categories: " + string.Join(", ", removedCats));

            var enCulture = new System.Globalization.CultureInfo("en-US");
            string dateStr = DateTime.Now.ToString("dd MMM yyyy, hh:mm tt", enCulture);

            string editSummary = changes.Any()
                ? string.Join(" | ", changes) + $" (edited on {dateStr})"
                : $"Profile re-saved with no changes (on {dateStr})";

            var shopCmd = new Microsoft.Data.SqlClient.SqlCommand(
     "UPDATE SellerShops SET ShopName = @shop, OwnerName = @owner, SubCategories = @categories, SellerPhone = @phone2, IsApproved = 0, AdminStatus = 'Pending', LastEditSummary = @summary WHERE SellerEmail = @email", con);
            shopCmd.Parameters.AddWithValue("@shop", model.ShopName ?? "");
            shopCmd.Parameters.AddWithValue("@owner", model.FullName ?? "");
            shopCmd.Parameters.AddWithValue("@categories", model.Categories ?? "");
            shopCmd.Parameters.AddWithValue("@phone2", model.Phone ?? "");
            shopCmd.Parameters.AddWithValue("@summary", editSummary);
            shopCmd.Parameters.AddWithValue("@email", email);
            shopCmd.ExecuteNonQuery();

            HttpContext.Session.SetString("ShopName", model.ShopName ?? "");

            TempData["ProfileSuccess"] = "Profile updated! Your shop is under admin review again.";
            return RedirectToAction("Profile");
        }
    }
}