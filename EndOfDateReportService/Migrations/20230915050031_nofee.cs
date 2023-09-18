using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EndOfDateReportService.Migrations
{
    public partial class nofee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EFTPOSFee",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "Gst",
                table: "Branches");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "EFTPOSFee",
                table: "Branches",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Gst",
                table: "Branches",
                type: "double precision",
                nullable: true);
        }
    }
}
