using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadmapAI.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadmapLearningSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseMaterials_CourseId",
                table: "CourseMaterials");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CourseMaterials",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CourseProgressSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CompletionPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseProgressSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseProgressSnapshots_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseProgressSnapshots_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseRoadmaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SummaryMarkdown = table.Column<string>(type: "text", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseRoadmaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseRoadmaps_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GenerationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GenerationJobs_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoadmapModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseRoadmapId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadmapModules_CourseRoadmaps_CourseRoadmapId",
                        column: x => x.CourseRoadmapId,
                        principalTable: "CourseRoadmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModuleCompletions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadmapModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleCompletions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleCompletions_RoadmapModules_RoadmapModuleId",
                        column: x => x.RoadmapModuleId,
                        principalTable: "RoadmapModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoadmapExams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadmapModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    IsFinal = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapExams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadmapExams_RoadmapModules_RoadmapModuleId",
                        column: x => x.RoadmapModuleId,
                        principalTable: "RoadmapModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoadmapLessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadmapModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    ContentMarkdown = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapLessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadmapLessons_RoadmapModules_RoadmapModuleId",
                        column: x => x.RoadmapModuleId,
                        principalTable: "RoadmapModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoadmapExamAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadmapExamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScorePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapExamAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadmapExamAttempts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoadmapExamAttempts_RoadmapExams_RoadmapExamId",
                        column: x => x.RoadmapExamId,
                        principalTable: "RoadmapExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoadmapExamQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadmapExamId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    QuestionText = table.Column<string>(type: "text", nullable: false),
                    OptionA = table.Column<string>(type: "text", nullable: false),
                    OptionB = table.Column<string>(type: "text", nullable: false),
                    OptionC = table.Column<string>(type: "text", nullable: false),
                    OptionD = table.Column<string>(type: "text", nullable: false),
                    CorrectOption = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    SourceMaterialId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapExamQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadmapExamQuestions_RoadmapExams_RoadmapExamId",
                        column: x => x.RoadmapExamId,
                        principalTable: "RoadmapExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonCompletions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadmapLessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonCompletions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonCompletions_RoadmapLessons_RoadmapLessonId",
                        column: x => x.RoadmapLessonId,
                        principalTable: "RoadmapLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoadmapExamAttemptAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadmapExamAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadmapExamQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedOption = table.Column<string>(type: "text", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapExamAttemptAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadmapExamAttemptAnswers_RoadmapExamAttempts_RoadmapExamAt~",
                        column: x => x.RoadmapExamAttemptId,
                        principalTable: "RoadmapExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoadmapExamAttemptAnswers_RoadmapExamQuestions_RoadmapExamQ~",
                        column: x => x.RoadmapExamQuestionId,
                        principalTable: "RoadmapExamQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_CourseId_Status",
                table: "CourseMaterials",
                columns: new[] { "CourseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseProgressSnapshots_CourseId_UserId",
                table: "CourseProgressSnapshots",
                columns: new[] { "CourseId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseProgressSnapshots_UserId",
                table: "CourseProgressSnapshots",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRoadmaps_CourseId",
                table: "CourseRoadmaps",
                column: "CourseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GenerationJobs_CourseId_Status_CreatedAt",
                table: "GenerationJobs",
                columns: new[] { "CourseId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonCompletions_RoadmapLessonId_UserId",
                table: "LessonCompletions",
                columns: new[] { "RoadmapLessonId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonCompletions_UserId",
                table: "LessonCompletions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleCompletions_RoadmapModuleId_UserId",
                table: "ModuleCompletions",
                columns: new[] { "RoadmapModuleId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleCompletions_UserId",
                table: "ModuleCompletions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapExamAttemptAnswers_RoadmapExamAttemptId",
                table: "RoadmapExamAttemptAnswers",
                column: "RoadmapExamAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapExamAttemptAnswers_RoadmapExamQuestionId",
                table: "RoadmapExamAttemptAnswers",
                column: "RoadmapExamQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapExamAttempts_RoadmapExamId_UserId_SubmittedAt",
                table: "RoadmapExamAttempts",
                columns: new[] { "RoadmapExamId", "UserId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapExamAttempts_UserId",
                table: "RoadmapExamAttempts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapExamQuestions_RoadmapExamId_OrderIndex",
                table: "RoadmapExamQuestions",
                columns: new[] { "RoadmapExamId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapExams_RoadmapModuleId",
                table: "RoadmapExams",
                column: "RoadmapModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapLessons_RoadmapModuleId_OrderIndex",
                table: "RoadmapLessons",
                columns: new[] { "RoadmapModuleId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapLessons_RoadmapModuleId_Status",
                table: "RoadmapLessons",
                columns: new[] { "RoadmapModuleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapModules_CourseRoadmapId_OrderIndex",
                table: "RoadmapModules",
                columns: new[] { "CourseRoadmapId", "OrderIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseProgressSnapshots");

            migrationBuilder.DropTable(
                name: "GenerationJobs");

            migrationBuilder.DropTable(
                name: "LessonCompletions");

            migrationBuilder.DropTable(
                name: "ModuleCompletions");

            migrationBuilder.DropTable(
                name: "RoadmapExamAttemptAnswers");

            migrationBuilder.DropTable(
                name: "RoadmapLessons");

            migrationBuilder.DropTable(
                name: "RoadmapExamAttempts");

            migrationBuilder.DropTable(
                name: "RoadmapExamQuestions");

            migrationBuilder.DropTable(
                name: "RoadmapExams");

            migrationBuilder.DropTable(
                name: "RoadmapModules");

            migrationBuilder.DropTable(
                name: "CourseRoadmaps");

            migrationBuilder.DropIndex(
                name: "IX_CourseMaterials_CourseId_Status",
                table: "CourseMaterials");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CourseMaterials");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_CourseId",
                table: "CourseMaterials",
                column: "CourseId");
        }
    }
}
