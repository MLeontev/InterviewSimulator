using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Interview.Infrastructure.Implementation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                schema: "Interview",
                table: "InterviewQuestions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                schema: "Interview",
                table: "InterviewQuestions");
        }
    }
}
