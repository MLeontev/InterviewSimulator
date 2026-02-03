using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Interview.Infrastructure.Implementation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPresetInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterviewPresetId",
                schema: "Interview",
                table: "InterviewSessions");

            migrationBuilder.AddColumn<string>(
                name: "InterviewPresetName",
                schema: "Interview",
                table: "InterviewSessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProgrammingLanguageCode",
                schema: "Interview",
                table: "InterviewSessions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterviewPresetName",
                schema: "Interview",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "ProgrammingLanguageCode",
                schema: "Interview",
                table: "InterviewSessions");

            migrationBuilder.AddColumn<Guid>(
                name: "InterviewPresetId",
                schema: "Interview",
                table: "InterviewSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
