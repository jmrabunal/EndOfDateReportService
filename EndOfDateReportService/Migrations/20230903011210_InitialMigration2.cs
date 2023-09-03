using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EndOfDateReportService.Migrations
{
    public partial class InitialMigration2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentMethods_BranchId_ReportDate_LaneId",
                table: "PaymentMethods");

            migrationBuilder.AddColumn<int>(
                name: "LaneId1",
                table: "PaymentMethods",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId1",
                table: "Lanes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_BranchId",
                table: "PaymentMethods",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_LaneId1",
                table: "PaymentMethods",
                column: "LaneId1");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_Name_LaneId_BranchId_ReportDate",
                table: "PaymentMethods",
                columns: new[] { "Name", "LaneId", "BranchId", "ReportDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lanes_BranchId1",
                table: "Lanes",
                column: "BranchId1");

            migrationBuilder.CreateIndex(
                name: "IX_Lanes_Id",
                table: "Lanes",
                column: "Id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Lanes_Branches_BranchId1",
                table: "Lanes",
                column: "BranchId1",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentMethods_Lanes_LaneId1",
                table: "PaymentMethods",
                column: "LaneId1",
                principalTable: "Lanes",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lanes_Branches_BranchId1",
                table: "Lanes");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentMethods_Lanes_LaneId1",
                table: "PaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_PaymentMethods_BranchId",
                table: "PaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_PaymentMethods_LaneId1",
                table: "PaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_PaymentMethods_Name_LaneId_BranchId_ReportDate",
                table: "PaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_Lanes_BranchId1",
                table: "Lanes");

            migrationBuilder.DropIndex(
                name: "IX_Lanes_Id",
                table: "Lanes");

            migrationBuilder.DropColumn(
                name: "LaneId1",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "BranchId1",
                table: "Lanes");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_BranchId_ReportDate_LaneId",
                table: "PaymentMethods",
                columns: new[] { "BranchId", "ReportDate", "LaneId" },
                unique: true);
        }
    }
}
