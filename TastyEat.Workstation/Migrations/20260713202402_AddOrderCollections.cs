using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TastyEat.Workstation.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderCollections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderCollections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderCollectionClients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderCollectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderCollectionClients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderCollectionClients_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderCollectionClients_OrderCollections_OrderCollectionId",
                        column: x => x.OrderCollectionId,
                        principalTable: "OrderCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderCollectionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderCollectionClientId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderCollectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderCollectionItems_OrderCollectionClients_OrderCollectionClientId",
                        column: x => x.OrderCollectionClientId,
                        principalTable: "OrderCollectionClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderCollectionItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderCollectionClients_ClientId",
                table: "OrderCollectionClients",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCollectionClients_OrderCollectionId",
                table: "OrderCollectionClients",
                column: "OrderCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCollectionItems_OrderCollectionClientId",
                table: "OrderCollectionItems",
                column: "OrderCollectionClientId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCollectionItems_ProductId",
                table: "OrderCollectionItems",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderCollectionItems");

            migrationBuilder.DropTable(
                name: "OrderCollectionClients");

            migrationBuilder.DropTable(
                name: "OrderCollections");
        }
    }
}
