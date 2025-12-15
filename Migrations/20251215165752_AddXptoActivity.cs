using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fitquest.Migrations
{
    /// <inheritdoc />
    public partial class AddXptoActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FullXp",
                table: "Activities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "XpAwarded",
                table: "Activities",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullXp",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "XpAwarded",
                table: "Activities");
        }
    }
}
