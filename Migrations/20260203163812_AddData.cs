using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fitquest.Migrations
{
    /// <inheritdoc />
    public partial class AddData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivityId",
                table: "TrainerActivityAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTrainerAssigned",
                table: "Activities",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TrainerActivityAssignments_ActivityId",
                table: "TrainerActivityAssignments",
                column: "ActivityId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainerActivityAssignments_Activities_ActivityId",
                table: "TrainerActivityAssignments",
                column: "ActivityId",
                principalTable: "Activities",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainerActivityAssignments_Activities_ActivityId",
                table: "TrainerActivityAssignments");

            migrationBuilder.DropIndex(
                name: "IX_TrainerActivityAssignments_ActivityId",
                table: "TrainerActivityAssignments");

            migrationBuilder.DropColumn(
                name: "ActivityId",
                table: "TrainerActivityAssignments");

            migrationBuilder.DropColumn(
                name: "IsTrainerAssigned",
                table: "Activities");
        }
    }
}
