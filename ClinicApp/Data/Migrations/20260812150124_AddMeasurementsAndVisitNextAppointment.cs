using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMeasurementsAndVisitNextAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ArmsInches",
                table: "DailyLogs",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChestInches",
                table: "DailyLogs",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "HipsInches",
                table: "DailyLogs",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "WaistInches",
                table: "DailyLogs",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ArmsInches",
                table: "ClientProfiles",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChestInches",
                table: "ClientProfiles",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "HipsInches",
                table: "ClientProfiles",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "WaistInches",
                table: "ClientProfiles",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAppointmentDate",
                table: "AttendanceRecords",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArmsInches",
                table: "DailyLogs");

            migrationBuilder.DropColumn(
                name: "ChestInches",
                table: "DailyLogs");

            migrationBuilder.DropColumn(
                name: "HipsInches",
                table: "DailyLogs");

            migrationBuilder.DropColumn(
                name: "WaistInches",
                table: "DailyLogs");

            migrationBuilder.DropColumn(
                name: "ArmsInches",
                table: "ClientProfiles");

            migrationBuilder.DropColumn(
                name: "ChestInches",
                table: "ClientProfiles");

            migrationBuilder.DropColumn(
                name: "HipsInches",
                table: "ClientProfiles");

            migrationBuilder.DropColumn(
                name: "WaistInches",
                table: "ClientProfiles");

            migrationBuilder.DropColumn(
                name: "NextAppointmentDate",
                table: "AttendanceRecords");
        }
    }
}
