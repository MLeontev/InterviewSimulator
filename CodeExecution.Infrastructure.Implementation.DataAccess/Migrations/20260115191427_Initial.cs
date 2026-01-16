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
                    Code = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    MaxTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    MaxMemoryMb = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    Input = table.Column<string>(type: "text", nullable: false),
                    ExpectedOutput = table.Column<string>(type: "text", nullable: false),
                    ActualOutput = table.Column<string>(type: "text", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: false),
                    ExitCode = table.Column<int>(type: "integer", nullable: false),
                    TimeElapsed = table.Column<double>(type: "double precision", nullable: false),
                    MemoryUsage = table.Column<double>(type: "double precision", nullable: false),
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
                name: "IX_CodeSubmissions_CreatedAt",
                schema: "CodeExecution",
                table: "CodeSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSubmissions_Status",
                schema: "CodeExecution",
                table: "CodeSubmissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSubmissionTestCases_SubmissionId",
                schema: "CodeExecution",
                table: "CodeSubmissionTestCases",
                column: "SubmissionId");
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
