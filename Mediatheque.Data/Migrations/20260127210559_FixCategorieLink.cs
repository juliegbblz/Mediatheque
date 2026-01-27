using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Mediatheque.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCategorieLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategorieActiviteId1",
                table: "Entrainements",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CouleurHex", "Nom" },
                values: new object[,]
                {
                    { 1, "#FF5733", "Musculation" },
                    { 2, "#2ECC71", "Cardio" },
                    { 3, "#9B59B6", "Yoga" },
                    { 4, "#3498DB", "Sport Collectif" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entrainements_CategorieActiviteId1",
                table: "Entrainements",
                column: "CategorieActiviteId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Entrainements_Categories_CategorieActiviteId1",
                table: "Entrainements",
                column: "CategorieActiviteId1",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Entrainements_Categories_CategorieActiviteId1",
                table: "Entrainements");

            migrationBuilder.DropIndex(
                name: "IX_Entrainements_CategorieActiviteId1",
                table: "Entrainements");

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "CategorieActiviteId1",
                table: "Entrainements");
        }
    }
}
