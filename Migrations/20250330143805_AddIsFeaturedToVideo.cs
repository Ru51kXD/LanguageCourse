using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication15.Migrations
{
    /// <inheritdoc />
    public partial class AddIsFeaturedToVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Videos",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Videos");
        }
    }
}
