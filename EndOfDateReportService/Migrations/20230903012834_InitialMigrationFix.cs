using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EndOfDateReportService.Migrations
{
    public partial class InitialMigrationFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lanes_Branches_BranchId",
                table: "Lanes");

            migrationBuilder.DropForeignKey(
                name: "FK_Lanes_Branches_BranchId1",
                table: "Lanes");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentMethods_Branches_BranchId",
                table: "PaymentMethods");

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
                name: "IX_Lanes_BranchId1",
                table: "Lanes");

            migrationBuilder.DropColumn(
                name: "LaneId1",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "BranchId1",
                table: "Lanes");

            migrationBuilder.AddForeignKey(
                name: "FK_Lanes_Branches_BranchId",
                table: "Lanes",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lanes_Branches_BranchId",
                table: "Lanes");

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
                name: "IX_Lanes_BranchId1",
                table: "Lanes",
                column: "BranchId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Lanes_Branches_BranchId",
                table: "Lanes",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lanes_Branches_BranchId1",
                table: "Lanes",
                column: "BranchId1",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentMethods_Branches_BranchId",
                table: "PaymentMethods",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentMethods_Lanes_LaneId1",
                table: "PaymentMethods",
                column: "LaneId1",
                principalTable: "Lanes",
                principalColumn: "Id");
        }
    }
}
