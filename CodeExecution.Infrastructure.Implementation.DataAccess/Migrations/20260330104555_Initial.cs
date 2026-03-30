using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeExecution.Infrastructure.Implementation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "CodeExecution");

            migrationBuilder.CreateTable(
                name: "CodeSubmissions",
                schema: "CodeExecution",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InterviewQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    LanguageCode = table.Column<string>(type: "text", nullable: false),
                    TimeLimitMs = table.Column<int>(type: "integer", nullable: false),
                    MemoryLimitMb = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    OverallVerdict = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsEventPublished = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodeSubmissionTestCases",
                schema: "CodeExecution",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Input = table.Column<string>(type: "text", nullable: false),
                    ExpectedOutput = table.Column<string>(type: "text", nullable: false),
                    ActualOutput = table.Column<string>(type: "text", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    ExitCode = table.Column<int>(type: "integer", nullable: true),
                    TimeElapsedMs = table.Column<double>(type: "double precision", nullable: true),
                    MemoryUsedMb = table.Column<double>(type: "double precision", nullable: true),
                    Verdict = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSubmissionTestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeSubmissionTestCases_CodeSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalSchema: "CodeExecution",
                        principalTable: "CodeSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeSubmissionTestCases_SubmissionId_OrderIndex",
                schema: "CodeExecution",
                table: "CodeSubmissionTestCases",
                columns: new[] { "SubmissionId", "OrderIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeSubmissionTestCases",
                schema: "CodeExecution");

            migrationBuilder.DropTable(
                name: "CodeSubmissions",
                schema: "CodeExecution");
        }
    }
}
