using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EndOfDateReportService.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lanes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lanes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lanes_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActualAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReportedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalVariance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LaneId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Name);
                    table.ForeignKey(
                        name: "FK_PaymentMethods_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentMethods_Lanes_LaneId",
                        column: x => x.LaneId,
                        principalTable: "Lanes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Branches",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Moore Wilsons Wellington" },
                    { 2, "Moore Wilsons Porirua" },
                    { 3, "Moore Wilsons Lower Hutt" },
                    { 4, "Moore Wilsons Masterton" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lanes_BranchId",
                table: "Lanes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_BranchId_ReportDate_LaneId",
                table: "PaymentMethods",
                columns: new[] { "BranchId", "ReportDate", "LaneId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_LaneId",
                table: "PaymentMethods",
                column: "LaneId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropTable(
                name: "Lanes");

            migrationBuilder.DropTable(
                name: "Branches");
        }
    }
}
