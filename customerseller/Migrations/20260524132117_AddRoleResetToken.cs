using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace customerseller.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleResetToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "bb");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "bbb");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "bear");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "bg");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "blue");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "boot");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "cc");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "db");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "dc");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "dd");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "ee");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "ff");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "gg");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "ggg");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "gs");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "hh");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "ii");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "jj");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "kb");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "kc");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "kk");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "ll");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "mb");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "nn");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "oo");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "pp");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "red");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "rr");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "sa");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "sb");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "tc");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "td");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "te");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "tf");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "tom");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "tt");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "wd");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

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
        }
    }
}
