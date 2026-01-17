using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeExecution.Infrastructure.Implementation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                schema: "CodeExecution",
                table: "CodeSubmissionTestCases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                schema: "CodeExecution",
                table: "CodeSubmissions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsEventPublished",
                schema: "CodeExecution",
                table: "CodeSubmissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OverallVerdict",
                schema: "CodeExecution",
                table: "CodeSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                schema: "CodeExecution",
                table: "CodeSubmissionTestCases");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                schema: "CodeExecution",
                table: "CodeSubmissions");

            migrationBuilder.DropColumn(
                name: "IsEventPublished",
                schema: "CodeExecution",
                table: "CodeSubmissions");

            migrationBuilder.DropColumn(
                name: "OverallVerdict",
                schema: "CodeExecution",
                table: "CodeSubmissions");
        }
    }
}
