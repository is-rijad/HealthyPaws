using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyPaws.Database.Migrations
{
    /// <inheritdoc />
    public partial class NextMeasureAtFieldAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextMeasureAt",
                table: "DogPlans",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextMeasureAt",
                table: "DogPlans");
        }
    }
}
