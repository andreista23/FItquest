using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Fitquest.Migrations
{
    /// <inheritdoc />
    public partial class SeedBadges_Level5_to_45 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Badges",
                columns: new[] { "Id", "Criteria", "Description", "ImagePath", "Title" },
                values: new object[,]
                {
                    { 1, "Reach level 5", "Ai ajuns la nivelul 5!", "/images/badges/level5.jpeg", "Level 5" },
                    { 2, "Reach level 10", "Ai ajuns la nivelul 10!", "/images/badges/level10.jpeg", "Level 10" },
                    { 3, "Reach level 15", "Ai ajuns la nivelul 15!", "/images/badges/level15.jpeg", "Level 15" },
                    { 4, "Reach level 20", "Ai ajuns la nivelul 20!", "/images/badges/level20.jpeg", "Level 20" },
                    { 5, "Reach level 25", "Ai ajuns la nivelul 25!", "/images/badges/level25.jpeg", "Level 25" },
                    { 6, "Reach level 30", "Ai ajuns la nivelul 30!", "/images/badges/level30.jpeg", "Level 30" },
                    { 7, "Reach level 35", "Ai ajuns la nivelul 35!", "/images/badges/level35.jpeg", "Level 35" },
                    { 8, "Reach level 40", "Ai ajuns la nivelul 40!", "/images/badges/level40.jpeg", "Level 40" },
                    { 9, "Reach level 45", "Ai ajuns la nivelul 45!", "/images/badges/level45.jpeg", "Level 45" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 9);
        }
    }
}
