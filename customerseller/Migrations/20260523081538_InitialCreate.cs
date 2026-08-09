using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace customerseller.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CancelledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrackingId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdditionalImages = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rating = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    reviews = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sizes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stock = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResetTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId");
                });

            migrationBuilder.CreateTable(
                name: "TrackingSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackingSteps_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "AdditionalImages", "Category", "Description", "DisplayId", "DisplayName", "ImageUrl", "Price", "Rating", "Stock", "Title", "reviews", "sizes" },
                values: new object[,]
                {
                    { "bb", null, "Shoes", null, "AV-012", null, "bb.jpg", 1850m, null, null, "Hat & Shoes Set", null, null },
                    { "bbb", null, "Bags", null, "AV-040", null, "bbb.jpg", 1950m, null, null, "Sunny Sunflower Tote", null, null },
                    { "bear", null, "Toys", null, "AV-147", null, "bear.jfif", 1550m, null, null, "Blue Bear and Pink Pig Plush", null, null },
                    { "bg", null, "Shoes", null, "AV-013", null, "bg.jpg", 2350m, null, null, "Vintage Brown Boots", null, null },
                    { "blue", null, "Shoes", null, "AV-004", null, "blue.jpg", 1450m, null, null, "Brown Sandals", null, null },
                    { "boot", null, "Shoes", null, "AV-005", null, "boot.jpg", 2100m, null, null, "Beige Block Heel Boots", null, null },
                    { "cc", null, "Bags", null, "AV-041", null, "cc.jpg", 2250m, null, null, "Soft Knit Handbag", null, null },
                    { "db", null, "HomeDecor", null, "AV-164", null, "db.jfif", 1290m, null, null, "Daisy Candle Set", null, null },
                    { "dc", null, "HomeDecor", null, "AV-165", null, "dc.jfif", 1190m, null, null, "Star Wall Hanging", null, null },
                    { "dd", null, "Bags", null, "AV-042", null, "dd.jpg", 1690m, null, null, "Brown Summer Tote", null, null },
                    { "ee", null, "Bags", null, "AV-043", null, "ee.jpg", 2100m, null, null, "Pastel Woven Tote", null, null },
                    { "ff", null, "Bags", null, "AV-045", null, "ff.jpg", 1890m, null, null, "Vivid Boho Crossbody", null, null },
                    { "gg", null, "Shoes", null, "AV-009", null, "gg.jpg", 2490m, null, null, "Metallic Sneakers Set", null, null },
                    { "ggg", null, "Bags", null, "AV-044", null, "ggg.jpg", 2750m, null, null, "Blush Knit Purse", null, null },
                    { "gs", null, "Shoes", null, "AV-003", null, "gs.jpg", 1700m, null, null, "Nude Heels", null, null },
                    { "hh", null, "Bags", null, "AV-046", null, "hh.jpg", 2090m, null, null, "Classic Straw Tote", null, null },
                    { "ii", null, "Bags", null, "AV-047", null, "ii.jpg", 2450m, null, null, "Sunset Chevron Bag", null, null },
                    { "jj", null, "Bags", null, "AV-048", null, "jj.jpg", 2350m, null, null, "Emerald Chic Tote", null, null },
                    { "kb", null, "Kitchen", null, "AV-175", null, "kb.jfif", 2190m, null, null, "Wooden Spiral Platter", null, null },
                    { "kc", null, "Kitchen", null, "AV-176", null, "kc.jfif", 1890m, null, null, "Wooden Plate Rack Set", null, null },
                    { "kk", null, "Bags", null, "AV-049", null, "kk.jpg", 2150m, null, null, "Golden Lace Satchel", null, null },
                    { "ll", null, "Bags", null, "AV-050", null, "ll.jpg", 1990m, null, null, "Lemon Chic Clutch", null, null },
                    { "mb", null, "Shoes", null, "AV-007", null, "mb.jpg", 2650m, null, null, "Brown Mens Boots", null, null },
                    { "nn", null, "Home", null, "AV-154", null, "nn.jpg", 2850m, null, null, "Green Swirl Plates", null, null },
                    { "oo", null, "Home", null, "AV-155", null, "oo.jpg", 2100m, null, null, "Rustic Spoon Bundle", null, null },
                    { "pp", null, "Home", null, "AV-156", null, "pp.jpg", 1990m, null, null, "Wicker Tray Decor", null, null },
                    { "red", null, "Shoes", null, "AV-006", null, "red.jpg", 1950m, null, null, "Maroon Heels", null, null },
                    { "rr", null, "Shoes", null, "AV-008", null, "rr.jpg", 1590m, null, null, "Burgundy Rain Boots", null, null },
                    { "sa", null, "Shoes", null, "AV-001", null, "sa.jfif", 1850m, null, null, "Multicolor Khussa", null, null },
                    { "sb", null, "Shoes", null, "AV-002", null, "sb.jfif", 2200m, null, null, "Brown Winter Boots", null, null },
                    { "tc", null, "Toys", null, "AV-139", null, "tc.jpg", 1500m, null, null, "White Teddy Bear", null, null },
                    { "td", null, "Toys", null, "AV-140", null, "td.jpg", 1800m, null, null, "Teddy Bear Couple", null, null },
                    { "te", null, "Toys", null, "AV-144", null, "te.jpg", 1400m, null, null, "Knitted Toy Trio", null, null },
                    { "tf", null, "Toys", null, "AV-142", null, "tf.jpg", 1700m, null, null, "Brown Teddy Pair", null, null },
                    { "tom", null, "Toys", null, "AV-148", null, "tom.jfif", 1750m, null, null, "Tom and Jerry Crochet Dolls", null, null },
                    { "tt", null, "Home", null, "AV-159", null, "tt.jpg", 2550m, null, null, "Artisan Brown Vase", null, null },
                    { "wd", null, "Shoes", null, "AV-010", null, "wd.jpg", 1990m, null, null, "Formal Shoes (Men)", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_OrderId",
                table: "CartItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSteps_OrderId",
                table: "TrackingSteps",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "TrackingSteps");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
