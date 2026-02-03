using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Interview.Infrastructure.Implementation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class OneAnswerField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeAnswer",
                schema: "Interview",
                table: "InterviewQuestions");

            migrationBuilder.RenameColumn(
                name: "TextAnswer",
                schema: "Interview",
                table: "InterviewQuestions",
                newName: "Answer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Answer",
                schema: "Interview",
                table: "InterviewQuestions",
                newName: "TextAnswer");

            migrationBuilder.AddColumn<string>(
                name: "CodeAnswer",
                schema: "Interview",
                table: "InterviewQuestions",
                type: "text",
                nullable: true);
        }
    }
}
