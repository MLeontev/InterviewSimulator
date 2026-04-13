using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Interview.Infrastructure.Implementation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddRetryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AiNextRetryAt",
                schema: "Interview",
                table: "InterviewSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AiRetryCount",
                schema: "Interview",
                table: "InterviewSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "AiNextRetryAt",
                schema: "Interview",
                table: "InterviewQuestions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AiRetryCount",
                schema: "Interview",
                table: "InterviewQuestions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiNextRetryAt",
                schema: "Interview",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "AiRetryCount",
                schema: "Interview",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "AiNextRetryAt",
                schema: "Interview",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "AiRetryCount",
                schema: "Interview",
                table: "InterviewQuestions");
        }
    }
}
