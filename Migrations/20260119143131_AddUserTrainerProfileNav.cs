using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fitquest.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTrainerProfileNav : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainerProfiles_UserId",
                table: "TrainerProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerProfiles_UserId",
                table: "TrainerProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainerProfiles_UserId",
                table: "TrainerProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerProfiles_UserId",
                table: "TrainerProfiles",
                column: "UserId");
        }
    }
}
