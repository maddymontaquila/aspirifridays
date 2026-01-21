using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BingoBoard.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBingoSquares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BingoSquares",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BingoSquares", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BingoSquares_IsActive",
                table: "BingoSquares",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BingoSquares_Type",
                table: "BingoSquares",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BingoSquares");
        }
    }
}
