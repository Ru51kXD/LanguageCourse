using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication15.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelsStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LanguageLevels_Languages_LanguageId",
                table: "LanguageLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_Tests_LanguageLevels_LanguageLevelId",
                table: "Tests");

            migrationBuilder.DropForeignKey(
                name: "FK_Tests_LanguageLevels_LanguageLevelId1",
                table: "Tests");

            migrationBuilder.DropForeignKey(
                name: "FK_Videos_LanguageLevels_LanguageLevelId",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Tests_LanguageLevelId1",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "LanguageLevelId1",
                table: "Tests");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "Videos",
                newName: "VideoUrl");

            migrationBuilder.RenameColumn(
                name: "CompletedDate",
                table: "TestResults",
                newName: "CompletionDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Videos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "Videos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Videos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "Videos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Tests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Tests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PassingScore",
                table: "Tests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "TestResults",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<int>(
                name: "CorrectAnswers",
                table: "TestResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPassed",
                table: "TestResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "CorrectAnswer",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Languages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2)",
                oldMaxLength: 2);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Languages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LanguageId1",
                table: "LanguageLevels",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "WatchedVideos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VideoId = table.Column<int>(type: "int", nullable: false),
                    WatchedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchedVideos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WatchedVideos_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WatchedVideos_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LanguageLevels_LanguageId1",
                table: "LanguageLevels",
                column: "LanguageId1");

            migrationBuilder.CreateIndex(
                name: "IX_WatchedVideos_UserId",
                table: "WatchedVideos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchedVideos_VideoId",
                table: "WatchedVideos",
                column: "VideoId");

            migrationBuilder.AddForeignKey(
                name: "FK_LanguageLevels_Languages_LanguageId",
                table: "LanguageLevels",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LanguageLevels_Languages_LanguageId1",
                table: "LanguageLevels",
                column: "LanguageId1",
                principalTable: "Languages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tests_LanguageLevels_LanguageLevelId",
                table: "Tests",
                column: "LanguageLevelId",
                principalTable: "LanguageLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_LanguageLevels_LanguageLevelId",
                table: "Videos",
                column: "LanguageLevelId",
                principalTable: "LanguageLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LanguageLevels_Languages_LanguageId",
                table: "LanguageLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_LanguageLevels_Languages_LanguageId1",
                table: "LanguageLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_Tests_LanguageLevels_LanguageLevelId",
                table: "Tests");

            migrationBuilder.DropForeignKey(
                name: "FK_Videos_LanguageLevels_LanguageLevelId",
                table: "Videos");

            migrationBuilder.DropTable(
                name: "WatchedVideos");

            migrationBuilder.DropIndex(
                name: "IX_LanguageLevels_LanguageId1",
                table: "LanguageLevels");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "PassingScore",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "CorrectAnswers",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "IsPassed",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "LanguageId1",
                table: "LanguageLevels");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegistrationDate",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "VideoUrl",
                table: "Videos",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "CompletionDate",
                table: "TestResults",
                newName: "CompletedDate");

            migrationBuilder.AddColumn<int>(
                name: "LanguageLevelId1",
                table: "Tests",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "TestResults",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "CorrectAnswer",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Languages",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Tests_LanguageLevelId1",
                table: "Tests",
                column: "LanguageLevelId1");

            migrationBuilder.AddForeignKey(
                name: "FK_LanguageLevels_Languages_LanguageId",
                table: "LanguageLevels",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tests_LanguageLevels_LanguageLevelId",
                table: "Tests",
                column: "LanguageLevelId",
                principalTable: "LanguageLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tests_LanguageLevels_LanguageLevelId1",
                table: "Tests",
                column: "LanguageLevelId1",
                principalTable: "LanguageLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_LanguageLevels_LanguageLevelId",
                table: "Videos",
                column: "LanguageLevelId",
                principalTable: "LanguageLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
