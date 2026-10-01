using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lavva_backend_clean.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSizeCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SizeCodes",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            // Existing products start with no size codes: an empty list, not NULL, so they read as one.
            migrationBuilder.Sql("UPDATE Products SET SizeCodes = N'[]' WHERE SizeCodes IS NULL;");

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "OrderItems",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "OrderItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SizeCodes",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "OrderItems");
        }
    }
}
