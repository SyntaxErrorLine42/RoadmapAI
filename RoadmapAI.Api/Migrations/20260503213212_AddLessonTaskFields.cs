using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadmapAI.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonTaskFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaskCorrectOption",
                table: "RoadmapLessons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskOptionA",
                table: "RoadmapLessons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskOptionB",
                table: "RoadmapLessons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskOptionC",
                table: "RoadmapLessons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskOptionD",
                table: "RoadmapLessons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskQuestionText",
                table: "RoadmapLessons",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaskCorrectOption",
                table: "RoadmapLessons");

            migrationBuilder.DropColumn(
                name: "TaskOptionA",
                table: "RoadmapLessons");

            migrationBuilder.DropColumn(
                name: "TaskOptionB",
                table: "RoadmapLessons");

            migrationBuilder.DropColumn(
                name: "TaskOptionC",
                table: "RoadmapLessons");

            migrationBuilder.DropColumn(
                name: "TaskOptionD",
                table: "RoadmapLessons");

            migrationBuilder.DropColumn(
                name: "TaskQuestionText",
                table: "RoadmapLessons");
        }
    }
}
