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

        // ==================== CUSTOMER SIDE ACTIONS ====================
        [HttpPost]
        public IActionResult Submit(Review review)
        {
            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                // Pehle product se SellerEmail nikalo
                var sellerCmd = new SqlCommand(
                    "SELECT SellerEmail FROM Products WHERE Id = @pid", con);
                sellerCmd.Parameters.AddWithValue("@pid", review.ProductId ?? "");
                var sellerEmail = sellerCmd.ExecuteScalar()?.ToString() ?? "";

                var cmd = new SqlCommand(
                    "INSERT INTO Reviews (ProductId, CustomerName, Rating, Comment, CreatedAt, SellerEmail) VALUES (@pid, @user, @rating, @comment, @created, @sellerEmail)",
                    con);

                cmd.Parameters.AddWithValue("@pid", review.ProductId ?? "");
                cmd.Parameters.AddWithValue("@user", review.UserName ?? "");
                cmd.Parameters.AddWithValue("@rating", review.Rating);
                cmd.Parameters.AddWithValue("@comment", review.Comment ?? "");
                cmd.Parameters.AddWithValue("@created", DateTime.Now);
                cmd.Parameters.AddWithValue("@sellerEmail", sellerEmail);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Details", "Products", new { id = review.ProductId });
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

        // ==================== SELLER SIDE ACTION (Fixed) ====================
        public IActionResult SellerReviews()
        {
            var sellerEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(sellerEmail))
                return RedirectToAction("Index", "Home");

            var reviews = new List<Review>();
            using (var con = new SqlConnection(_conn))
            {
                // ✅ Sirf is seller ke reviews lao
                var cmd = new SqlCommand(
                    "SELECT * FROM Reviews WHERE SellerEmail = @sellerEmail ORDER BY CreatedAt DESC", con);
                cmd.Parameters.AddWithValue("@sellerEmail", sellerEmail);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    reviews.Add(new Review
                    {
                        Id = (int)reader["Id"],
                        ProductId = reader["ProductId"].ToString(),
                        UserName = reader["CustomerName"].ToString(),
                        CustomerName = reader["CustomerName"].ToString(),
                        Rating = (int)reader["Rating"],
                        Comment = reader["Comment"].ToString(),
                        CreatedAt = (DateTime)reader["CreatedAt"]
                    });
                }
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