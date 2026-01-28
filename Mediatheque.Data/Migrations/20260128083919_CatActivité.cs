using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mediatheque.Data.Migrations
{
    /// <inheritdoc />
    public partial class CatActivité : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CouleurHex",
                value: "#E6B200");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CouleurHex",
                value: "#FFEB3B");
        }
    }
}
