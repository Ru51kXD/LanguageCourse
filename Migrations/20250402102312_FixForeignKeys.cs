using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication15.Migrations
{
    /// <inheritdoc />
    public partial class FixForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CompletionDate",
                table: "TestResults",
                newName: "CompletedDate");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "TestResults",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "TestResults",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxScore",
                table: "TestResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Percentage",
                table: "TestResults",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "TestResults",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "Percentage",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "TestResults");

            migrationBuilder.RenameColumn(
                name: "CompletedDate",
                table: "TestResults",
                newName: "CompletionDate");
        }
    }
}
