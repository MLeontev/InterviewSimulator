using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Interview.Infrastructure.Implementation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddVerdicts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SessionVerdict",
                schema: "Interview",
                table: "InterviewSessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QuestionVerdict",
                schema: "Interview",
                table: "InterviewQuestions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionVerdict",
                schema: "Interview",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "QuestionVerdict",
                schema: "Interview",
                table: "InterviewQuestions");
        }
    }
}
