using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fitquest.Migrations
{
    /// <inheritdoc />
    public partial class ExtendTrainerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CvPath",
                table: "TrainerProfiles",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "TrainerProfiles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RecommendationPath",
                table: "TrainerProfiles",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "TrainerProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TrainerProfiles_UserId",
                table: "TrainerProfiles",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainerProfiles_Users_UserId",
                table: "TrainerProfiles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainerProfiles_Users_UserId",
                table: "TrainerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_TrainerProfiles_UserId",
                table: "TrainerProfiles");

            migrationBuilder.DropColumn(
                name: "CvPath",
                table: "TrainerProfiles");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "TrainerProfiles");

            migrationBuilder.DropColumn(
                name: "RecommendationPath",
                table: "TrainerProfiles");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TrainerProfiles");
        }
    }
}
