using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyPaws.Database.Migrations
{
    /// <inheritdoc />
    public partial class DeviceMacAddressUniqueAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Devices_DogId",
                table: "Devices");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DogId_MacAddress",
                table: "Devices",
                columns: new[] { "DogId", "MacAddress" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Devices_DogId_MacAddress",
                table: "Devices");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DogId",
                table: "Devices",
                column: "DogId");
        }
    }
}
