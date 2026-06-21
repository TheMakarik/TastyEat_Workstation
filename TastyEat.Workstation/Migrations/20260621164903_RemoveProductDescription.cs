using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TastyEat.Workstation.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Products",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);
        }
    }
}
