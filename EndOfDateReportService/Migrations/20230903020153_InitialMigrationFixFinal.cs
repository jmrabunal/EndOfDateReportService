using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EndOfDateReportService.Migrations
{
    public partial class InitialMigrationFixFinal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lanes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<int>(type: "integer", nullable: false)
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
                    Name = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActualAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ReportedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalVariance = table.Column<decimal>(type: "numeric", nullable: false),
                    ReportDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LaneId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Name);
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
                name: "IX_Lanes_Id",
                table: "Lanes",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_LaneId",
                table: "PaymentMethods",
                column: "LaneId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_Name_LaneId_BranchId_ReportDate",
                table: "PaymentMethods",
                columns: new[] { "Name", "LaneId", "BranchId", "ReportDate" },
                unique: true);
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
