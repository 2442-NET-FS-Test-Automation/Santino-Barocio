using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KitchenFulfillment.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedDataConstants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Email", "Name" },
                values: new object[] { 1, "john.doe@example.com", "John Doe" });

            migrationBuilder.InsertData(
                table: "DiningTables",
                columns: new[] { "Id", "TableNumber" },
                values: new object[] { 1, 5 });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "Name", "Role" },
                values: new object[] { 1, "Chef Gordon", "Chef" });

            migrationBuilder.InsertData(
                table: "MenuCategories",
                columns: new[] { "Id", "Name" },
                values: new object[] { 1, "Main Course" });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "MenuCategoryId", "Name", "Price", "Sku" },
                values: new object[,]
                {
                    { 1, 1, "Lasagna", 15.99m, "LST-001" },
                    { 2, 1, "Pepperoni Pizza", 12.50m, "PPR-002" }
                });

            migrationBuilder.InsertData(
                table: "InventoryItems",
                columns: new[] { "Id", "MenuItemId", "QuantityOnHand" },
                values: new object[,]
                {
                    { 1, 1, 100 },
                    { 2, 2, 100 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "InventoryItems",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "InventoryItems",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "MenuCategories",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
