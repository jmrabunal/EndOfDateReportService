using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EndOfDateReportService.Migrations
{
    public partial class InitialMigrationFixFinal2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lanes_Id",
                table: "Lanes");

            migrationBuilder.AddColumn<int>(
                name: "LaneId",
                table: "Lanes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LaneId",
                table: "Lanes");

            migrationBuilder.CreateIndex(
                name: "IX_Lanes_Id",
                table: "Lanes",
                column: "Id",
                unique: true);
        }
    }
}
