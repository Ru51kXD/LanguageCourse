using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication15.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelsWithNewProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "TestResults",
                newName: "TestTitle");

            migrationBuilder.AddColumn<int>(
                name: "LanguageId",
                table: "Videos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LanguageId",
                table: "Tests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "TestResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuestionType",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Options",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "LanguageLevels",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "LanguageLevels",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_LanguageId",
                table: "Videos",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Tests_LanguageId",
                table: "Tests",
                column: "LanguageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tests_Languages_LanguageId",
                table: "Tests",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_Languages_LanguageId",
                table: "Videos",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tests_Languages_LanguageId",
                table: "Tests");

            migrationBuilder.DropForeignKey(
                name: "FK_Videos_Languages_LanguageId",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Videos_LanguageId",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Tests_LanguageId",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "QuestionType",
                table: "Questions");

            migrationBuilder.RenameColumn(
                name: "TestTitle",
                table: "TestResults",
                newName: "Title");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Options",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "LanguageLevels",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "LanguageLevels",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}
