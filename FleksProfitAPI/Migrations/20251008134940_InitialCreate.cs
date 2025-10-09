using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleksProfitAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FcrRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HourUTC = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HourDK = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FCRdomestic_MW = table.Column<double>(type: "REAL", nullable: true),
                    FCRabroad_MW = table.Column<double>(type: "REAL", nullable: true),
                    FCRcross_EUR = table.Column<double>(type: "REAL", nullable: true),
                    FCRcross_DKK = table.Column<double>(type: "REAL", nullable: true),
                    FCRdk_EUR = table.Column<double>(type: "REAL", nullable: true),
                    FCRdk_DKK = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FcrRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FcrRecords_HourUTC",
                table: "FcrRecords",
                column: "HourUTC",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FcrRecords");
        }
    }
}
