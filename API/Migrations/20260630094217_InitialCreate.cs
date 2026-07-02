using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Attendance_Service.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DependentsCount = table.Column<int>(type: "int", nullable: false),
                    IdentityNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BankBranch = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LeaveType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    GracePeriodMinutes = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    TotalStandardWorkdays = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    TotalAnnualLeaveDays = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    TotalSickLeaveDays = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    TotalUnpaidLeaveDays = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    TotalAbsentDays = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    TotalOvertimeHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TotalLateMinutes = table.Column<int>(type: "int", nullable: false),
                    TotalEarlyCheckOutMinutes = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceSummaries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckIn = table.Column<TimeSpan>(type: "time", nullable: true),
                    CheckOut = table.Column<TimeSpan>(type: "time", nullable: true),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LateCheckInMinutes = table.Column<int>(type: "int", nullable: false),
                    EarlyCheckOutMinutes = table.Column<int>(type: "int", nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    StandardWorkday = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceLogs_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Shifts",
                columns: new[] { "Id", "CreatedAt", "Description", "EndTime", "GracePeriodMinutes", "Name", "StartTime", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2026, 6, 30, 9, 42, 16, 348, DateTimeKind.Utc).AddTicks(4177), "Ca lÃ m viá»‡c chÃ­nh tá»« 8:00 Ä‘áº¿n 17:30", new TimeSpan(0, 17, 30, 0, 0), 15, "Ca HÃ nh chÃ­nh", new TimeSpan(0, 8, 0, 0, 0), "Active", new DateTime(2026, 6, 30, 9, 42, 16, 348, DateTimeKind.Utc).AddTicks(4183) },
                    { new Guid("00000000-0000-0000-0000-000000000002"), new DateTime(2026, 6, 30, 9, 42, 16, 348, DateTimeKind.Utc).AddTicks(8589), "Ca sÃ¡ng tá»« 7:00 Ä‘áº¿n 12:00", new TimeSpan(0, 12, 0, 0, 0), 15, "Ca SÃ¡ng", new TimeSpan(0, 7, 0, 0, 0), "Active", new DateTime(2026, 6, 30, 9, 42, 16, 348, DateTimeKind.Utc).AddTicks(8590) },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new DateTime(2026, 6, 30, 9, 42, 16, 348, DateTimeKind.Utc).AddTicks(8613), "Ca chiá»u tá»« 13:00 Ä‘áº¿n 18:00", new TimeSpan(0, 18, 0, 0, 0), 15, "Ca Chiá»u", new TimeSpan(0, 13, 0, 0, 0), "Active", new DateTime(2026, 6, 30, 9, 42, 16, 348, DateTimeKind.Utc).AddTicks(8614) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_Date",
                table: "AttendanceLogs",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_EmployeeId_Date",
                table: "AttendanceLogs",
                columns: new[] { "EmployeeId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_ShiftId",
                table: "AttendanceLogs",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSummaries_EmployeeId_Year_Month",
                table: "AttendanceSummaries",
                columns: new[] { "EmployeeId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Status",
                table: "LeaveRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceLogs");

            migrationBuilder.DropTable(
                name: "AttendanceSummaries");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "Employees");
        }
    }
}
