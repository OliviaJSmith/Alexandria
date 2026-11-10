using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alexandria.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanDueReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueTodayReminderSentAt",
                table: "Loans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueTomorrowReminderSentAt",
                table: "Loans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Loans_Status_DueDate",
                table: "Loans",
                columns: new[] { "Status", "DueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Loans_Status_DueDate",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "DueTodayReminderSentAt",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "DueTomorrowReminderSentAt",
                table: "Loans");
        }
    }
}
