using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Interview.Infrastructure.Implementation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MemoryLimitMb",
                schema: "Interview",
                table: "InterviewQuestions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeLimitMs",
                schema: "Interview",
                table: "InterviewQuestions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MemoryLimitMb",
                schema: "Interview",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "TimeLimitMs",
                schema: "Interview",
                table: "InterviewQuestions");
        }
    }
}
