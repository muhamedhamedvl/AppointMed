using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index on Appointments.Status for filtering by status
            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status",
                table: "Appointments",
                column: "Status");

            // Index on Appointments.AppointmentDate for date range queries
            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentDate",
                table: "Appointments",
                column: "AppointmentDate");

            // Composite index for common doctor query pattern (doctor + status + date)
            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_Status_AppointmentDate",
                table: "Appointments",
                columns: new[] { "DoctorId", "Status", "AppointmentDate" });

            // Composite index for email queue processing (status + created date)
            migrationBuilder.CreateIndex(
                name: "IX_EmailQueues_Status_CreatedAt",
                table: "EmailQueues",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailQueues_Status_CreatedAt",
                table: "EmailQueues");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId_Status_AppointmentDate",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_AppointmentDate",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_Status",
                table: "Appointments");
        }
    }
}
