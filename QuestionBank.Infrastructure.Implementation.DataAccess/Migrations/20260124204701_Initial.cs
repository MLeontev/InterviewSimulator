using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "QuestionBank");

            migrationBuilder.CreateTable(
                name: "Competencies",
                schema: "QuestionBank",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Grades",
                schema: "QuestionBank",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Specializations",
                schema: "QuestionBank",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specializations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Technologies",
                schema: "QuestionBank",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technologies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompetencyMatrices",
                schema: "QuestionBank",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecializationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencyMatrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetencyMatrices_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "QuestionBank",
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompetencyMatrices_Specializations_SpecializationId",
                        column: x => x.SpecializationId,
                        principalSchema: "QuestionBank",
                        principalTable: "Specializations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterviewPresets",
                schema: "QuestionBank",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    GradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecializationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewPresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewPresets_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "QuestionBank",
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewPresets_Specializations_SpecializationId",
                        column: x => x.SpecializationId,
                        principalSchema: "QuestionBank",
                        principalTable: "Specializations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                schema: "QuestionBank",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    ReferenceSolution = table.Column<string>(type: "text", nullable: false),
                    CompetencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    GradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_Competencies_CompetencyId",
                        column: x => x.CompetencyId,
                        principalSchema: "QuestionBank",
                        principalTable: "Competencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Questions_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "QuestionBank",
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Questions_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalSchema: "QuestionBank",
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetencyMatrixItems",
                schema: "QuestionBank",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    CompetencyMatrixId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencyMatrixItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetencyMatrixItems_Competencies_CompetencyId",
                        column: x => x.CompetencyId,
                        principalSchema: "QuestionBank",
                        principalTable: "Competencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompetencyMatrixItems_CompetencyMatrices_CompetencyMatrixId",
                        column: x => x.CompetencyMatrixId,
                        principalSchema: "QuestionBank",
                        principalTable: "CompetencyMatrices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InterviewPresetTechnologies",
                schema: "QuestionBank",
                columns: table => new
                {
                    InterviewPresetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewPresetTechnologies", x => new { x.InterviewPresetId, x.TechnologyId });
                    table.ForeignKey(
                        name: "FK_InterviewPresetTechnologies_InterviewPresets_InterviewPrese~",
                        column: x => x.InterviewPresetId,
                        principalSchema: "QuestionBank",
                        principalTable: "InterviewPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewPresetTechnologies_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalSchema: "QuestionBank",
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CodingQuestionLanguageLimits",
                schema: "QuestionBank",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodingQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeLimitMs = table.Column<int>(type: "integer", nullable: false),
                    MemoryLimitMb = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodingQuestionLanguageLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodingQuestionLanguageLimits_Questions_CodingQuestionId",
                        column: x => x.CodingQuestionId,
                        principalSchema: "QuestionBank",
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CodingQuestionLanguageLimits_Technologies_LanguageId",
                        column: x => x.LanguageId,
                        principalSchema: "QuestionBank",
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestCases",
                schema: "QuestionBank",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodingQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Input = table.Column<string>(type: "text", nullable: false),
                    ExpectedOutput = table.Column<string>(type: "text", nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestCases_Questions_CodingQuestionId",
                        column: x => x.CodingQuestionId,
                        principalSchema: "QuestionBank",
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodingQuestionLanguageLimits_CodingQuestionId",
                schema: "QuestionBank",
                table: "CodingQuestionLanguageLimits",
                column: "CodingQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_CodingQuestionLanguageLimits_LanguageId",
                schema: "QuestionBank",
                table: "CodingQuestionLanguageLimits",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyMatrices_GradeId_SpecializationId",
                schema: "QuestionBank",
                table: "CompetencyMatrices",
                columns: new[] { "GradeId", "SpecializationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyMatrices_SpecializationId",
                schema: "QuestionBank",
                table: "CompetencyMatrices",
                column: "SpecializationId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyMatrixItems_CompetencyId",
                schema: "QuestionBank",
                table: "CompetencyMatrixItems",
                column: "CompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyMatrixItems_CompetencyMatrixId",
                schema: "QuestionBank",
                table: "CompetencyMatrixItems",
                column: "CompetencyMatrixId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPresets_GradeId",
                schema: "QuestionBank",
                table: "InterviewPresets",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPresets_SpecializationId",
                schema: "QuestionBank",
                table: "InterviewPresets",
                column: "SpecializationId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPresetTechnologies_TechnologyId",
                schema: "QuestionBank",
                table: "InterviewPresetTechnologies",
                column: "TechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CompetencyId",
                schema: "QuestionBank",
                table: "Questions",
                column: "CompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_GradeId",
                schema: "QuestionBank",
                table: "Questions",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_TechnologyId",
                schema: "QuestionBank",
                table: "Questions",
                column: "TechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_CodingQuestionId",
                schema: "QuestionBank",
                table: "TestCases",
                column: "CodingQuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodingQuestionLanguageLimits",
                schema: "QuestionBank");

            migrationBuilder.DropTable(
                name: "CompetencyMatrixItems",
                schema: "QuestionBank");

            migrationBuilder.DropTable(
                name: "InterviewPresetTechnologies",
                schema: "QuestionBank");

            migrationBuilder.DropTable(
                name: "TestCases",
                schema: "QuestionBank");

            migrationBuilder.DropTable(
                name: "CompetencyMatrices",
                schema: "QuestionBank");

            migrationBuilder.DropTable(
                name: "InterviewPresets",
                schema: "QuestionBank");

            migrationBuilder.DropTable(
                name: "Questions",
                schema: "QuestionBank");

            migrationBuilder.DropTable(
                name: "Specializations",
                schema: "QuestionBank");

            migrationBuilder.DropTable(
                name: "Competencies",
                schema: "QuestionBank");

            migrationBuilder.DropTable(
                name: "Grades",
                schema: "QuestionBank");

            migrationBuilder.DropTable(
                name: "Technologies",
                schema: "QuestionBank");
        }
    }
}
