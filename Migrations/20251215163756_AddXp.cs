using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fitquest.Migrations
{
    /// <inheritdoc />
    public partial class AddXp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Xp",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Xp",
                table: "Users");
        }
    }
}
