using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexVision.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCompletedDateToSavedRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "SavedRoutes",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "SavedRoutes");
        }
    }
}
