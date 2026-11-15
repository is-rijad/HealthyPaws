using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyPaws.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLinUcbAndMore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "MealIndex",
                table: "DogMeals",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.CreateTable(
                name: "BanditDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DogId = table.Column<int>(type: "int", nullable: false),
                    MealIndex = table.Column<int>(type: "int", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    BaselineUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Arm = table.Column<int>(type: "int", nullable: true),
                    ChosenByFallback = table.Column<bool>(type: "bit", nullable: false),
                    ContextSavedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HoursSinceLastMeal = table.Column<double>(type: "float", nullable: true),
                    AvgEatenFraction = table.Column<double>(type: "float", nullable: true),
                    TreatBudgetShare = table.Column<double>(type: "float", nullable: true),
                    BaselineHour = table.Column<double>(type: "float", nullable: true),
                    DispensedG = table.Column<double>(type: "float", nullable: true),
                    EatenG = table.Column<double>(type: "float", nullable: true),
                    LatencyS = table.Column<double>(type: "float", nullable: true),
                    Reward = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ChosenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DispensedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FeedbackSentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanditDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanditDecisions_Dogs_DogId",
                        column: x => x.DogId,
                        principalTable: "Dogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BanditStates",
                columns: table => new
                {
                    DogId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    StateJson = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanditStates", x => x.DogId);
                    table.ForeignKey(
                        name: "FK_BanditStates_Dogs_DogId",
                        column: x => x.DogId,
                        principalTable: "Dogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BanditDecisions_DogId_BaselineUtc",
                table: "BanditDecisions",
                columns: new[] { "DogId", "BaselineUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BanditDecisions_DogId_MealIndex_Day",
                table: "BanditDecisions",
                columns: new[] { "DogId", "MealIndex", "Day" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BanditDecisions");

            migrationBuilder.DropTable(
                name: "BanditStates");

            migrationBuilder.DropColumn(
                name: "MealIndex",
                table: "DogMeals");
        }
    }
}
