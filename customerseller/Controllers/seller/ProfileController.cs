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

            // Users table se data lo
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

            // SellerShops se data lo
            var shopCmd = new Microsoft.Data.SqlClient.SqlCommand(
     "SELECT ShopName, CNIC, SubCategories, SellerPhone, SellerArea FROM SellerShops WHERE SellerEmail = @email", con);
            shopCmd.Parameters.AddWithValue("@email", email);
            var shopReader = shopCmd.ExecuteReader();

            string shopName = "", cnic = "", categories = "", sellerPhone = "", sellerArea = "";
            if (shopReader.Read())
            {
                shopName = shopReader["ShopName"]?.ToString() ?? "";
                cnic = shopReader["CNIC"]?.ToString() ?? "";
                categories = shopReader["SubCategories"]?.ToString() ?? "";
                sellerPhone = shopReader["SellerPhone"]?.ToString() ?? "";
                sellerArea = shopReader["SellerArea"]?.ToString() ?? "";
            }
            shopReader.Close();

            var profile = new UserProfile
            {
                FullName = fullName.Trim(),
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

            // Users table update karo
            var userCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "UPDATE Users SET Phone = @phone WHERE Email = @email", con);
            userCmd.Parameters.AddWithValue("@phone", model.Phone ?? "");
            userCmd.Parameters.AddWithValue("@email", email);
            userCmd.ExecuteNonQuery();

            // SellerShops update karo + Categories bhi + IsApproved = 0 (admin review)
            var shopCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "UPDATE SellerShops SET ShopName = @shop, SubCategories = @categories, IsApproved = 0 WHERE SellerEmail = @email", con);
            shopCmd.Parameters.AddWithValue("@shop", model.ShopName ?? "");
            shopCmd.Parameters.AddWithValue("@categories", model.Categories ?? "");
            shopCmd.Parameters.AddWithValue("@email", email);
            shopCmd.ExecuteNonQuery();

            // Session update karo
            HttpContext.Session.SetString("ShopName", model.ShopName ?? "");

            TempData["ProfileSuccess"] = "Profile updated! Your shop is under admin review again.";
            return RedirectToAction("Profile");
        }
    }
}