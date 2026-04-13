using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeExecution.Infrastructure.Implementation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewTestCaseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InterviewTestCaseId",
                schema: "CodeExecution",
                table: "CodeSubmissionTestCases",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterviewTestCaseId",
                schema: "CodeExecution",
                table: "CodeSubmissionTestCases");
        }
    }
}
