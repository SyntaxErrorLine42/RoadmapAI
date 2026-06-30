using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadmapAI.Api.Migrations
{
    public partial class AddExamAttemptSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrectOption",
                table: "RoadmapExamAttemptAnswers",
                type: "character varying(1)",
                maxLength: 1,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QuestionText",
                table: "RoadmapExamAttemptAnswers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectOption",
                table: "RoadmapExamAttemptAnswers");

            migrationBuilder.DropColumn(
                name: "QuestionText",
                table: "RoadmapExamAttemptAnswers");
        }
    }
}
