using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TastyEat.Workstation.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionAndWeightedProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWeighted",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<double>(
                name: "Quantity",
                table: "ProductionBatchItems",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "ProductionBatches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsWeighted",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "ProductionBatches");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "ProductionBatchItems",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");
        }
    }
}
