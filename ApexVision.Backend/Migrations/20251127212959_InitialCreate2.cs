using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexVision.Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "bbd5b38b-183f-4a55-b403-34bf9a8c703e", "AQAAAAIAAYagAAAAEFeU1LUXeoA1jlSlmcRAv7gcl5/NkTM4P2lCNmiMv4VAxqD12IPEEmKW4x6QIzTjTQ==", "0b4ae10b-ec53-4d15-bccf-69e807baf9e4" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "930c468b-0bb0-46ef-9901-207c79d0e8d9", "AQAAAAIAAYagAAAAEErLUZQyIunin3X0OjiQmEOGDh6FZFgnxcZR2T2tQ1JJTX1m6wAIn/NTo5EWJUIb8Q==", "d5cfff65-3779-4c36-8442-340fe63f9743" });
        }
    }
}
