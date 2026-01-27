using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mediatheque.Data.Migrations
{
    /// <inheritdoc />
    public partial class AjoutTableCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    CouleurHex = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Entrainements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Activite = table.Column<string>(type: "TEXT", nullable: false),
                    DateHeure = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Lieu = table.Column<string>(type: "TEXT", nullable: false),
                    DureeMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    CategorieActiviteId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entrainements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entrainements_Categories_CategorieActiviteId",
                        column: x => x.CategorieActiviteId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entrainements_CategorieActiviteId",
                table: "Entrainements",
                column: "CategorieActiviteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entrainements");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
