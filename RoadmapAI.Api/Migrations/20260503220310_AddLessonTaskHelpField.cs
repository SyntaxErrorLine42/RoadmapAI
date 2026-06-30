using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadmapAI.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonTaskHelpField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaskHelpMarkdown",
                table: "RoadmapLessons",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaskHelpMarkdown",
                table: "RoadmapLessons");
        }
    }
}
