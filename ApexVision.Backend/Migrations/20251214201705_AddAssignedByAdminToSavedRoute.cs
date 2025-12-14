using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexVision.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedByAdminToSavedRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedByAdminId",
                table: "SavedRoutes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedRoutes_AssignedByAdminId",
                table: "SavedRoutes",
                column: "AssignedByAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_SavedRoutes_AspNetUsers_AssignedByAdminId",
                table: "SavedRoutes",
                column: "AssignedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedRoutes_AspNetUsers_AssignedByAdminId",
                table: "SavedRoutes");

            migrationBuilder.DropIndex(
                name: "IX_SavedRoutes_AssignedByAdminId",
                table: "SavedRoutes");

            migrationBuilder.DropColumn(
                name: "AssignedByAdminId",
                table: "SavedRoutes");
        }
    }
}
