using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TastyEat.Workstation.Migrations
{
    /// <inheritdoc />
    public partial class RefactorDistributionClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributionItems_Clients_ClientId",
                table: "DistributionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributionItems_Distributions_DistributionId",
                table: "DistributionItems");

            migrationBuilder.DropColumn(
                name: "PriceAtDistribution",
                table: "DistributionItems");

            migrationBuilder.RenameColumn(
                name: "DistributionId",
                table: "DistributionItems",
                newName: "DistributionClientId");

            migrationBuilder.RenameIndex(
                name: "IX_DistributionItems_DistributionId",
                table: "DistributionItems",
                newName: "IX_DistributionItems_DistributionClientId");

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "DistributionItems",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateTable(
                name: "DistributionClients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DistributionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributionClients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributionClients_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DistributionClients_Distributions_DistributionId",
                        column: x => x.DistributionId,
                        principalTable: "Distributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DistributionClients_ClientId",
                table: "DistributionClients",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionClients_DistributionId",
                table: "DistributionClients",
                column: "DistributionId");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "DistributionItems");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributionItems_DistributionClients_DistributionClientId",
                table: "DistributionItems",
                column: "DistributionClientId",
                principalTable: "DistributionClients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributionItems_Clients_ClientId",
                table: "DistributionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributionItems_DistributionClients_DistributionClientId",
                table: "DistributionItems");

            migrationBuilder.DropTable(
                name: "DistributionClients");

            migrationBuilder.RenameColumn(
                name: "DistributionClientId",
                table: "DistributionItems",
                newName: "DistributionId");

            migrationBuilder.RenameIndex(
                name: "IX_DistributionItems_DistributionClientId",
                table: "DistributionItems",
                newName: "IX_DistributionItems_DistributionId");

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "DistributionItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PriceAtDistribution",
                table: "DistributionItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributionItems_Clients_ClientId",
                table: "DistributionItems",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributionItems_Distributions_DistributionId",
                table: "DistributionItems",
                column: "DistributionId",
                principalTable: "Distributions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
