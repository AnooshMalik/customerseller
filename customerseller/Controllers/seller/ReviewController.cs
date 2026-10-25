using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using customerseller.Models;

namespace customerseller.Controllers.seller
{
    public class ReviewController : Controller
    {
        private readonly string _conn;

        public ReviewController(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
        }

        [HttpPost]
        public IActionResult Submit(Review review)
        {
            var customerEmail = HttpContext.Session.GetString("UserEmail");
            var sessionName = HttpContext.Session.GetString("UserName");

            if (string.IsNullOrWhiteSpace(review.ProductId))
            {
                TempData["ReviewError"] = "Invalid product.";
                return RedirectToAction("Index", "Home");
            }

            string backUrl = "/Products/Details/" + review.ProductId + "#reviews";

            if (string.IsNullOrEmpty(customerEmail))
            {
                TempData["ReviewError"] = "Please login to submit a review.";
                return Redirect(backUrl);
            }

            if (review.Rating < 1 || review.Rating > 5)
            {
                TempData["ReviewError"] = "Rating must be between 1 and 5.";
                return Redirect(backUrl);
            }

            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                var purchaseCheckCmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM Orders o
                    INNER JOIN CartItems ci ON ci.OrderId = o.OrderId
                    WHERE o.Email = @email AND ci.ProductId = @pid AND o.Status = 'Delivered'", con);
                purchaseCheckCmd.Parameters.AddWithValue("@email", customerEmail);
                purchaseCheckCmd.Parameters.AddWithValue("@pid", review.ProductId);
                int purchaseCount = (int)purchaseCheckCmd.ExecuteScalar();

                if (purchaseCount == 0)
                {
                    TempData["ReviewError"] = "You can only review products you have purchased and received.";
                    return Redirect(backUrl);
                }

                var sellerCmd = new SqlCommand(
                    "SELECT SellerEmail FROM Products WHERE Id = @pid", con);
                sellerCmd.Parameters.AddWithValue("@pid", review.ProductId ?? "");
                var sellerEmail = sellerCmd.ExecuteScalar()?.ToString() ?? "";

                var cmd = new SqlCommand(
                    "INSERT INTO Reviews (ProductId, CustomerName, Rating, Comment, CreatedAt, SellerEmail) VALUES (@pid, @user, @rating, @comment, @created, @sellerEmail)",
                    con);

                cmd.Parameters.AddWithValue("@pid", review.ProductId ?? "");
                cmd.Parameters.AddWithValue("@user", sessionName ?? "Customer");
                cmd.Parameters.AddWithValue("@rating", review.Rating);
                cmd.Parameters.AddWithValue("@comment", review.Comment ?? "");
                cmd.Parameters.AddWithValue("@created", DateTime.Now);
                cmd.Parameters.AddWithValue("@sellerEmail", sellerEmail);
                cmd.ExecuteNonQuery();
            }

            return Redirect(backUrl);
        }
        public List<Review> GetByProduct(string productId)
        {
            var list = new List<Review>();
            using (var con = new SqlConnection(_conn))
            {
                var cmd = new SqlCommand(
                    "SELECT * FROM Reviews WHERE ProductId = @pid ORDER BY CreatedAt DESC", con);
                cmd.Parameters.AddWithValue("@pid", productId);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add(new Review
                    {
                        Id = (int)reader["Id"],
                        UserName = reader["CustomerName"].ToString(),
                        Rating = (int)reader["Rating"],
                        Comment = reader["Comment"].ToString(),
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        SellerResponse = reader["SellerResponse"] == DBNull.Value
    ? null : reader["SellerResponse"].ToString()
                    });
            }
            return list;
        }

        public IActionResult SellerReviews()
        {
            var sellerEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(sellerEmail))
                return RedirectToAction("Index", "Home");

            var reviews = new List<Review>();
            var rawData = new List<(int Id, string ProductId, string CustomerName, int Rating, string Comment, DateTime CreatedAt, string SellerResponse)>();

            using (var con = new SqlConnection(_conn))
            {
                var cmd = new SqlCommand(
                    "SELECT * FROM Reviews WHERE SellerEmail = @sellerEmail ORDER BY CreatedAt DESC", con);
                cmd.Parameters.AddWithValue("@sellerEmail", sellerEmail);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    rawData.Add((
                        (int)reader["Id"],
                        reader["ProductId"].ToString(),
                        reader["CustomerName"].ToString(),
                        (int)reader["Rating"],
                        reader["Comment"].ToString(),
                        (DateTime)reader["CreatedAt"],
                        reader["SellerResponse"] == DBNull.Value ? null : reader["SellerResponse"].ToString()
                    ));
                }
            }

            foreach (var r in rawData)
            {
                string productTitle = "Product";
                using (var con2 = new SqlConnection(_conn))
                {
                    var pcmd = new SqlCommand("SELECT Title FROM Products WHERE Id = @pid", con2);
                    pcmd.Parameters.AddWithValue("@pid", r.ProductId ?? "");
                    con2.Open();
                    productTitle = pcmd.ExecuteScalar()?.ToString() ?? "Product";
                }

                var customerName = string.IsNullOrEmpty(r.CustomerName) ? "Customer" : r.CustomerName;
                var initials = string.Join("", customerName.Split(' ')
                    .Where(w => w.Length > 0)
                    .Take(2)
                    .Select(w => char.ToUpper(w[0])));

                reviews.Add(new Review
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    ProductName = productTitle,
                    UserName = customerName,
                    CustomerName = customerName,
                    CustomerInitials = string.IsNullOrEmpty(initials) ? "U" : initials,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    DateLabel = r.CreatedAt.ToString("dd MMM yyyy", new System.Globalization.CultureInfo("en-US")),
                    SellerResponse = r.SellerResponse
                });
            }

            return View("~/Views/Seller/Review/SellerReviews.cshtml", reviews);
        }

        [HttpPost]
        public IActionResult RespondToReview([FromBody] ReviewResponseRequest request)
        {
            var sellerEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(sellerEmail))
                return Json(new { success = false });

            using var con = new SqlConnection(_conn);
            con.Open();
            var cmd = new SqlCommand(
                "UPDATE Reviews SET SellerResponse = @response WHERE Id = @id AND SellerEmail = @email", con);
            cmd.Parameters.AddWithValue("@response", request.Response ?? "");
            cmd.Parameters.AddWithValue("@id", request.ReviewId);
            cmd.Parameters.AddWithValue("@email", sellerEmail);
            cmd.ExecuteNonQuery();

            return Json(new { success = true });
        }

        public class ReviewResponseRequest
        {
            public int ReviewId { get; set; }
            public string Response { get; set; } = "";
        }
    }
}