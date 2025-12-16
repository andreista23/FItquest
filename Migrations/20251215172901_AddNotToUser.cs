using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fitquest.Migrations
{
    /// <inheritdoc />
    public partial class AddNotToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastNotifiedLevel",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastNotifiedLevel",
                table: "Users");
        }
    }
}
