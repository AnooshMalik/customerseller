using customerseller.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace customerseller.Controllers
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

            double sellerAvgRating = 0;
            var ratingCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT AVG(CAST(Rating AS FLOAT)) FROM Reviews WHERE SellerEmail = @email", con);
            ratingCmd.Parameters.AddWithValue("@email", email);
            var ratingResult = ratingCmd.ExecuteScalar();
            if (ratingResult != null && ratingResult != DBNull.Value)
                sellerAvgRating = Convert.ToDouble(ratingResult);
            var db = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var inv = System.Globalization.CultureInfo.InvariantCulture;

            var orderFeed = sellerOrders
     .Where(o => o.Status != "Cancelled" && o.Status != "Delivered")
     .Take(5)
                .Select(o => new
                {
                    When = o.OrderDate,
                    Item = new SaleActivity
                    {
                        Detail = $"🛒 Order #{o.OrderId} — {o.CustomerName} · {o.ProductTitle} x{o.Quantity} · {o.Status} · {o.OrderDate.ToString("dd MMM, h:mm tt", inv)}",
                        Amount = $"Rs. {(o.Price * o.Quantity):N0}"
                    }
                })
                .ToList();

            var complaintFeed = db.Complaints
                               .Where(c => c.SellerName.ToLower() == shopName.ToLower()
                         && c.Status != "Open" && c.Status != "Fake"
                         && c.Status != "Resolved")
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToList()
                .Select(c => new
                {
                    When = c.CreatedAt,
                    Item = new SaleActivity
                    {
                        Detail = $"🚩 Complaint {c.TicketNumber} — {c.CustomerName} · {c.CreatedAt.ToString("dd MMM, h:mm tt", inv)}",
                        Amount = c.Status == "Forwarded" ? "Response needed" : c.Status
                    }
                })
                .ToList();

            var recentFeed = orderFeed
                .Concat(complaintFeed)
                .OrderByDescending(x => x.When)
                .Take(8)
                .Select(x => x.Item)
                .ToList();
            var dashboardData = new DashboardViewModel
            {
                SellerName = HttpContext.Session.GetString("UserName") ?? "",
                ShopName = shopName,
                AccountStatus = categories,
                NetRevenue = sellerOrders.Where(o => o.Status == "Delivered").Sum(o => o.Price * o.Quantity),
                RevenueGrowth = 0,
                TotalOrders = sellerOrders.Count,
                PendingOrders = sellerOrders.Count(o => o.Status == "Pending" || o.Status == "Placed" || o.Status == "Processing"),
                Rating = Math.Round(sellerAvgRating, 1),
                LiveVisitors = 0,
                RecentSales = recentFeed,
                SellerOrders = sellerOrders  
            };

            return View("~/Views/Seller/Dashboard/Dashboard.cshtml", dashboardData);
        }

        public IActionResult Customers()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(email) || role != "Seller")
                return RedirectToAction("Index", "Home");

            var rows = new List<CustomerOrderRow>();

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                HttpContext.RequestServices
                    .GetRequiredService<ApplicationDbContext>()
                    .Database.GetConnectionString());
            con.Open();

            var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT o.OrderId, o.FirstName, o.LastName, o.CustomerPhone, o.CustomerAddress, o.Address, o.Status, o.OrderDate,
ci.Title, ci.Quantity, ci.Price
                FROM Orders o
                INNER JOIN CartItems ci ON o.OrderId = ci.OrderId
                INNER JOIN Products p ON ci.ProductId = p.Id
                WHERE p.SellerEmail = @email
                ORDER BY o.OrderDate DESC", con);
            cmd.Parameters.AddWithValue("@email", email);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                rows.Add(new CustomerOrderRow
                {
                    OrderId = r["OrderId"].ToString() ?? "",
                    CustomerName = r["FirstName"] + " " + r["LastName"],
                    Phone = r["CustomerPhone"]?.ToString() ?? "",
                    Address = !string.IsNullOrWhiteSpace(r["CustomerAddress"]?.ToString())
                                ? r["CustomerAddress"].ToString()!
                                : (r["Address"]?.ToString() ?? ""),
                    ProductTitle = r["Title"].ToString() ?? "",
                    Quantity = Convert.ToInt32(r["Quantity"]),
                    Price = Convert.ToDecimal(r["Price"]),
                    Status = r["Status"]?.ToString() ?? "",
                    OrderDate = Convert.ToDateTime(r["OrderDate"])
                });
            }

            return View("~/Views/Seller/Dashboard/Customers.cshtml", rows);
        }
    }
}

    
