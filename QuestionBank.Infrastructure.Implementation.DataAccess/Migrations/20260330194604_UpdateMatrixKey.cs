using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMatrixKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InterviewPresetCompetencies",
                schema: "QuestionBank",
                table: "InterviewPresetCompetencies");

            migrationBuilder.DropIndex(
                name: "IX_InterviewPresetCompetencies_InterviewPresetId_CompetencyId",
                schema: "QuestionBank",
                table: "InterviewPresetCompetencies");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: "QuestionBank",
                table: "InterviewPresetCompetencies");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterviewPresetCompetencies",
                schema: "QuestionBank",
                table: "InterviewPresetCompetencies",
                columns: new[] { "InterviewPresetId", "CompetencyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InterviewPresetCompetencies",
                schema: "QuestionBank",
                table: "InterviewPresetCompetencies");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                schema: "QuestionBank",
                table: "InterviewPresetCompetencies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterviewPresetCompetencies",
                schema: "QuestionBank",
                table: "InterviewPresetCompetencies",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPresetCompetencies_InterviewPresetId_CompetencyId",
                schema: "QuestionBank",
                table: "InterviewPresetCompetencies",
                columns: new[] { "InterviewPresetId", "CompetencyId" },
                unique: true);
        }
    }
}
