using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyPaws.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTreats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Treats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, collation: "Croatian_CI_AS"),
                    CaloriesPerPiece = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Treats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Treats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TreatLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DogId = table.Column<int>(type: "int", nullable: false),
                    TreatId = table.Column<int>(type: "int", nullable: true),
                    TreatName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, collation: "Croatian_CI_AS"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Calories = table.Column<double>(type: "float", nullable: false),
                    GivenAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatLogs_Dogs_DogId",
                        column: x => x.DogId,
                        principalTable: "Dogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TreatLogs_Treats_TreatId",
                        column: x => x.TreatId,
                        principalTable: "Treats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TreatLogs_DogId_GivenAt",
                table: "TreatLogs",
                columns: new[] { "DogId", "GivenAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatLogs_TreatId",
                table: "TreatLogs",
                column: "TreatId");

            migrationBuilder.CreateIndex(
                name: "IX_Treats_UserId_Name",
                table: "Treats",
                columns: new[] { "UserId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TreatLogs");

            migrationBuilder.DropTable(
                name: "Treats");
        }
    }
}
