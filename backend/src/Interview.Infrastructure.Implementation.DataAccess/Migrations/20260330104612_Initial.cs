using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Interview.Infrastructure.Implementation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Interview");

            migrationBuilder.CreateTable(
                name: "InterviewSessions",
                schema: "Interview",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterviewPresetId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterviewPresetName = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedEndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AiFeedbackJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterviewQuestions",
                schema: "Interview",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InterviewSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: true),
                    ReferenceSolution = table.Column<string>(type: "text", nullable: false),
                    AiFeedbackJson = table.Column<string>(type: "text", nullable: true),
                    ProgrammingLanguageCode = table.Column<string>(type: "text", nullable: true),
                    OverallVerdict = table.Column<string>(type: "text", nullable: false),
                    TimeLimitMs = table.Column<int>(type: "integer", nullable: true),
                    MemoryLimitMb = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewQuestions_InterviewSessions_InterviewSessionId",
                        column: x => x.InterviewSessionId,
                        principalSchema: "Interview",
                        principalTable: "InterviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestCases",
                schema: "Interview",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InterviewQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Input = table.Column<string>(type: "text", nullable: false),
                    ExpectedOutput = table.Column<string>(type: "text", nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    ActualOutput = table.Column<string>(type: "text", nullable: true),
                    ExecutionTimeMs = table.Column<double>(type: "double precision", nullable: true),
                    MemoryUsedMb = table.Column<double>(type: "double precision", nullable: true),
                    Verdict = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestCases_InterviewQuestions_InterviewQuestionId",
                        column: x => x.InterviewQuestionId,
                        principalSchema: "Interview",
                        principalTable: "InterviewQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_InterviewSessionId_OrderIndex",
                schema: "Interview",
                table: "InterviewQuestions",
                columns: new[] { "InterviewSessionId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_CandidateId",
                schema: "Interview",
                table: "InterviewSessions",
                column: "CandidateId",
                unique: true,
                filter: "\"Status\" = 'InProgress'");

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_InterviewQuestionId_OrderIndex",
                schema: "Interview",
                table: "TestCases",
                columns: new[] { "InterviewQuestionId", "OrderIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestCases",
                schema: "Interview");

            migrationBuilder.DropTable(
                name: "InterviewQuestions",
                schema: "Interview");

            migrationBuilder.DropTable(
                name: "InterviewSessions",
                schema: "Interview");
        }
    }
}
