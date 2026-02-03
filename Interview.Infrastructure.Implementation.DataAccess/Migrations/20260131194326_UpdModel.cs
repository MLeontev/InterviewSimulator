using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Interview.Infrastructure.Implementation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestCase_InterviewQuestions_InterviewQuestionId",
                schema: "Interview",
                table: "TestCase");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestCase",
                schema: "Interview",
                table: "TestCase");

            migrationBuilder.DropColumn(
                name: "InterviewPresetName",
                schema: "Interview",
                table: "InterviewSessions");

            migrationBuilder.RenameTable(
                name: "TestCase",
                schema: "Interview",
                newName: "TestCases",
                newSchema: "Interview");

            migrationBuilder.RenameIndex(
                name: "IX_TestCase_InterviewQuestionId",
                schema: "Interview",
                table: "TestCases",
                newName: "IX_TestCases_InterviewQuestionId");

            migrationBuilder.AddColumn<Guid>(
                name: "InterviewPresetId",
                schema: "Interview",
                table: "InterviewSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "OverallVerdict",
                schema: "Interview",
                table: "InterviewQuestions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Verdict",
                schema: "Interview",
                table: "TestCases",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestCases",
                schema: "Interview",
                table: "TestCases",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestCases_InterviewQuestions_InterviewQuestionId",
                schema: "Interview",
                table: "TestCases",
                column: "InterviewQuestionId",
                principalSchema: "Interview",
                principalTable: "InterviewQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestCases_InterviewQuestions_InterviewQuestionId",
                schema: "Interview",
                table: "TestCases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestCases",
                schema: "Interview",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "InterviewPresetId",
                schema: "Interview",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "OverallVerdict",
                schema: "Interview",
                table: "InterviewQuestions");

            migrationBuilder.RenameTable(
                name: "TestCases",
                schema: "Interview",
                newName: "TestCase",
                newSchema: "Interview");

            migrationBuilder.RenameIndex(
                name: "IX_TestCases_InterviewQuestionId",
                schema: "Interview",
                table: "TestCase",
                newName: "IX_TestCase_InterviewQuestionId");

            migrationBuilder.AddColumn<string>(
                name: "InterviewPresetName",
                schema: "Interview",
                table: "InterviewSessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "Verdict",
                schema: "Interview",
                table: "TestCase",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestCase",
                schema: "Interview",
                table: "TestCase",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestCase_InterviewQuestions_InterviewQuestionId",
                schema: "Interview",
                table: "TestCase",
                column: "InterviewQuestionId",
                principalSchema: "Interview",
                principalTable: "InterviewQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
