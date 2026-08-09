using customerseller.Models;
using Microsoft.Data.SqlClient;

using Microsoft.Data.SqlClient;   
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using customerseller.Models;
 
namespace customerseller.Services
{
    public class ReviewService
    {
        private readonly string _conn;

        public ReviewService(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
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
                        CreatedAt = (DateTime)reader["CreatedAt"]
                    });
            }
            return list;
        }
    }
}









