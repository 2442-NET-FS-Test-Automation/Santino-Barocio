using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KitchenFulfillment.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Email", "Name" },
                values: new object[,]
                {
                    { 2, "maria.garcia@example.com", "Maria Garcia" },
                    { 3, "carlos.ruiz@example.com", "Carlos Ruiz" }
                });

            migrationBuilder.InsertData(
                table: "DiningTables",
                columns: new[] { "Id", "TableNumber" },
                values: new object[] { 2, 12 });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "Name", "Role" },
                values: new object[] { 2, "Maria Lopez", "Waiter" });

            migrationBuilder.InsertData(
                table: "MenuCategories",
                columns: new[] { "Id", "Name" },
                values: new object[] { 2, "Appetizer" });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "MenuCategoryId", "Name", "Price", "Sku" },
                values: new object[] { 4, 1, "Grilled Salmon", 22.50m, "GRL-004" });

            migrationBuilder.InsertData(
                table: "InventoryItems",
                columns: new[] { "Id", "MenuItemId", "QuantityOnHand" },
                values: new object[] { 4, 4, 100 });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "MenuCategoryId", "Name", "Price", "Sku" },
                values: new object[,]
                {
                    { 3, 2, "Caesar Salad", 9.99m, "CSR-003" },
                    { 5, 2, "Tacos al Pastor", 11.00m, "TCS-005" }
                });

            migrationBuilder.InsertData(
                table: "InventoryItems",
                columns: new[] { "Id", "MenuItemId", "QuantityOnHand" },
                values: new object[,]
                {
                    { 3, 3, 100 },
                    { 5, 5, 100 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "InventoryItems",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "InventoryItems",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "InventoryItems",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "MenuCategories",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
