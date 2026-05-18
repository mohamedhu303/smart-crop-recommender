using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriAnalytics.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgriDataRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Temperature = table.Column<float>(type: "real", nullable: false),
                    Humidity = table.Column<float>(type: "real", nullable: false),
                    Soil_pH = table.Column<float>(type: "real", nullable: false),
                    Rainfall = table.Column<float>(type: "real", nullable: false),
                    CropLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateRecorded = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgriDataRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgriDataRecords_CropLabel",
                table: "AgriDataRecords",
                column: "CropLabel");

            migrationBuilder.CreateIndex(
                name: "IX_AgriDataRecords_DateRecorded",
                table: "AgriDataRecords",
                column: "DateRecorded");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgriDataRecords");
        }
    }
}
