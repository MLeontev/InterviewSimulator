using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Interview.Infrastructure.Implementation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompetencyId",
                schema: "Interview",
                table: "InterviewQuestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompetencyName",
                schema: "Interview",
                table: "InterviewQuestions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompetencyId",
                schema: "Interview",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "CompetencyName",
                schema: "Interview",
                table: "InterviewQuestions");
        }
    }
}
