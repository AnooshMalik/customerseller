using customerseller.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SELLERMVC.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Dashboard()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Index", "Home");

            if (role != "Seller")
                return RedirectToAction("Index", "Home");

            string shopName = "";
            string categories = "";

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                HttpContext.RequestServices
                    .GetRequiredService<ApplicationDbContext>()
                    .Database.GetConnectionString());

            con.Open();

            var checkCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT COUNT(*) FROM SellerShops WHERE SellerEmail = @email", con);
            checkCmd.Parameters.AddWithValue("@email", email);
            int shopCount = (int)checkCmd.ExecuteScalar();

            if (shopCount == 0)
                return RedirectToAction("SellerTerms", "Account");

            var shopCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT ShopName, SubCategories, IsApproved FROM SellerShops WHERE SellerEmail = @email", con);
            shopCmd.Parameters.AddWithValue("@email", email);

            using var reader = shopCmd.ExecuteReader();
            if (reader.Read())
            {
                shopName = reader["ShopName"]?.ToString() ?? "";
                categories = reader["SubCategories"]?.ToString() ?? "";

                bool isApproved = (bool)reader["IsApproved"];

                if (!isApproved)
                {
                    reader.Close();
                    TempData["PendingNotice"] = "Your shop is pending admin approval. You can upload products in the meantime.";
                    return RedirectToAction("AddProduct", "Products");
               }
            }
            reader.Close();

           
            var rejectedAlerts = new List<object>();

            var modRejectedCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT SubCategory, RejectionReason FROM SubCategoryVideos WHERE SellerEmail = @email AND VideoStatus = 'Rejected'", con);
            modRejectedCmd.Parameters.AddWithValue("@email", email);
            var modReader = modRejectedCmd.ExecuteReader();
            while (modReader.Read())
            {
                rejectedAlerts.Add(new
                {
                    SubCategory = modReader["SubCategory"].ToString(),
                    Reason = modReader["RejectionReason"]?.ToString() ?? "No reason provided",
                    RejectedBy = "Moderator"
                });
            }
            modReader.Close();

            var shopCmd2 = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT RejectedSubCategories, SubCategoryRejectionReasons FROM SellerShops WHERE SellerEmail = @email", con);
            shopCmd2.Parameters.AddWithValue("@email", email);
            var shopReader2 = shopCmd2.ExecuteReader();
            if (shopReader2.Read())
            {
                var rejectedSubs = shopReader2["RejectedSubCategories"]?.ToString() ?? "";
                var reasons = shopReader2["SubCategoryRejectionReasons"]?.ToString() ?? "";

                if (!string.IsNullOrEmpty(rejectedSubs))
                {
                    var subList = rejectedSubs.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var reasonList = reasons.Split('|', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var sub in subList)
                    {
                        var matchingReason = reasonList.FirstOrDefault(r => r.StartsWith(sub + ":"));
                        var reasonText = matchingReason != null ? matchingReason.Substring(sub.Length + 1) : "No reason provided";

                        rejectedAlerts.Add(new
                        {
                            SubCategory = sub.Trim(),
                            Reason = reasonText,
                            RejectedBy = "Admin"
                        });
                    }
                }
            }
            shopReader2.Close();

            
            var rejectedProducts = new List<object>();
            var rejectedProdCmd = new Microsoft.Data.SqlClient.SqlCommand(
                @"SELECT p.Id, p.Title, p.AdminRejectionReason 
      FROM Products p
      INNER JOIN SubCategoryVideos sv ON p.SellerEmail = sv.SellerEmail 
                                      AND p.Category = sv.SubCategory
      WHERE p.SellerEmail = @email 
      AND p.IsAdminApproved = 0 
      AND p.AdminRejectionReason IS NOT NULL
      AND sv.VideoStatus = 'Approved'", con);
            rejectedProdCmd.Parameters.AddWithValue("@email", email);
            var prodReader = rejectedProdCmd.ExecuteReader();
            while (prodReader.Read())
            {
                rejectedProducts.Add(new
                {
                    Id = prodReader["Id"].ToString(),
                    Title = prodReader["Title"].ToString(),
                    Reason = prodReader["AdminRejectionReason"]?.ToString() ?? "No reason provided",
                    RejectedBy = "Admin"
                });
            }
            prodReader.Close();

            ViewBag.RejectedProducts = rejectedProducts;
           

            ViewBag.RejectedAlerts = rejectedAlerts;

            HttpContext.Session.SetString("ShopName", shopName);

            ViewBag.RejectedAlerts = rejectedAlerts;

            HttpContext.Session.SetString("ShopName", shopName);

           
            var sellerOrders = new List<SellerOrderItem>();
            var ordersCmd = new Microsoft.Data.SqlClient.SqlCommand(@"
    SELECT o.OrderId, o.FirstName, o.LastName, o.Status, o.OrderDate,
           ci.Title, ci.Quantity, ci.Price
    FROM Orders o
    INNER JOIN CartItems ci ON o.OrderId = ci.OrderId
    INNER JOIN Products p ON ci.ProductId = p.Id
    WHERE p.SellerEmail = @email
    ORDER BY o.OrderDate DESC", con);
            ordersCmd.Parameters.AddWithValue("@email", email);
            var ordersReader = ordersCmd.ExecuteReader();
            while (ordersReader.Read())
            {
                sellerOrders.Add(new SellerOrderItem
                {
                    OrderId = ordersReader["OrderId"].ToString(),
                    CustomerName = ordersReader["FirstName"] + " " + ordersReader["LastName"],
                    ProductTitle = ordersReader["Title"].ToString(),
                    Quantity = Convert.ToInt32(ordersReader["Quantity"]),
                    Price = Convert.ToDecimal(ordersReader["Price"]),
                    Status = ordersReader["Status"].ToString(),
                    OrderDate = Convert.ToDateTime(ordersReader["OrderDate"])
                });
            }
            ordersReader.Close();

            var dashboardData = new DashboardViewModel
            {
                SellerName = HttpContext.Session.GetString("UserName") ?? "",
                ShopName = shopName,
                AccountStatus = categories,
                NetRevenue = sellerOrders.Sum(o => o.Price * o.Quantity),  
                RevenueGrowth = 0,
                TotalOrders = sellerOrders.Count,  
                PendingOrders = sellerOrders.Count(o => o.Status == "Processing"),  
                Rating = 0.0,
                LiveVisitors = 0,
                RecentSales = new List<SaleActivity>(),
                SellerOrders = sellerOrders  
            };

            return View("~/Views/Seller/Dashboard/Dashboard.cshtml", dashboardData);
        }



    }
}