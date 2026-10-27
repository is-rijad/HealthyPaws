using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyPaws.Database.Migrations
{
    /// <inheritdoc />
    public partial class DogCalorieBudgetAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DailyCalorieBudget",
                table: "Dogs",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyCalorieBudget",
                table: "Dogs");
        }
    }
}
