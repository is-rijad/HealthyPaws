using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyPaws.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDogWeightCalorieSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CalorieSource",
                table: "DogWeights",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalorieSource",
                table: "DogWeights");
        }
    }
}
