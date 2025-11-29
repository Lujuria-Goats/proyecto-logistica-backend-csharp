using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexVision.Backend.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_Logic_New : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "059a85dc-362e-4314-8ebf-ed296f0d49fd", "AQAAAAIAAYagAAAAEBramUyEPGzC3qMrScogITBCpONLE328OF2o24rkEHJpVGxOPXZrPqWyqZYt5+vhCw==", "4570275c-190f-4e44-add4-57ad3a435fa8" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "84a16af3-a482-41d7-96a4-25a9efa59a4d", "AQAAAAIAAYagAAAAENKKW8CIvvmfiVypqa/HZLWz863AREHif4weT8r42JeNoEPVH4sgQHN/AoPmtxeTug==", "2933d8c0-30b2-4dbe-91a6-d72359ac390a" });
        }
    }
}
