using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadmapAI.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMaterialSummaryColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Summary",
                table: "CourseMaterials");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "CourseMaterials",
                type: "text",
                nullable: true);
        }
    }
}
