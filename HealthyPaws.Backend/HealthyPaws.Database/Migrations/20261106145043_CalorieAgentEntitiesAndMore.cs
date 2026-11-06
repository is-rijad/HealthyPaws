using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyPaws.Database.Migrations
{
    /// <inheritdoc />
    public partial class CalorieAgentEntitiesAndMore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "EatenGPerDay",
                table: "DogWeights",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OfferedGPerDay",
                table: "DogWeights",
                type: "float",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentDecisionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DogId = table.Column<int>(type: "int", nullable: false),
                    MeasuredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    ReasonCodes = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    NewDailyKcal = table.Column<double>(type: "float", nullable: true),
                    NewDailyGrams = table.Column<double>(type: "float", nullable: true),
                    PlanVersionInResponse = table.Column<int>(type: "int", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsRejected = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDecisionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DogId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Occurrences = table.Column<int>(type: "int", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DogPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DogId = table.Column<int>(type: "int", nullable: false),
                    DailyKcal = table.Column<double>(type: "float", nullable: false),
                    DailyGrams = table.Column<double>(type: "float", nullable: false),
                    PlanVersion = table.Column<int>(type: "int", nullable: false),
                    LastAppliedMeasuredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DogPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeedingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DogId = table.Column<int>(type: "int", nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    EatenG = table.Column<double>(type: "float", nullable: false),
                    EatenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OfferedG = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedingRecords_DogPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "DogPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedingRecords_Dogs_DogId",
                        column: x => x.DogId,
                        principalTable: "Dogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearnedStates",
                columns: table => new
                {
                    DogPlanId = table.Column<int>(type: "int", nullable: false),
                    MerEstimateKcal = table.Column<double>(type: "float", nullable: true),
                    VarianceKcal2 = table.Column<double>(type: "float", nullable: true),
                    NUpdates = table.Column<int>(type: "int", nullable: false),
                    ConsecutiveBadWeeks = table.Column<int>(type: "int", nullable: false),
                    WeeksAtBound = table.Column<int>(type: "int", nullable: false),
                    PendingWeightKg = table.Column<double>(type: "float", nullable: true),
                    LastUpdateAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnedStates", x => x.DogPlanId);
                    table.ForeignKey(
                        name: "FK_LearnedStates_DogPlans_DogPlanId",
                        column: x => x.DogPlanId,
                        principalTable: "DogPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentDecisionLogs_DogId_MeasuredAt",
                table: "AgentDecisionLogs",
                columns: new[] { "DogId", "MeasuredAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_DogId_Reason",
                table: "Alerts",
                columns: new[] { "DogId", "Reason" },
                unique: true,
                filter: "[ResolvedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FeedingRecords_DogId_EatenAt",
                table: "FeedingRecords",
                columns: new[] { "DogId", "EatenAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeedingRecords_PlanId",
                table: "FeedingRecords",
                column: "PlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentDecisionLogs");

            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "FeedingRecords");

            migrationBuilder.DropTable(
                name: "LearnedStates");

            migrationBuilder.DropTable(
                name: "DogPlans");

            migrationBuilder.DropColumn(
                name: "EatenGPerDay",
                table: "DogWeights");

            migrationBuilder.DropColumn(
                name: "OfferedGPerDay",
                table: "DogWeights");
        }
    }
}
