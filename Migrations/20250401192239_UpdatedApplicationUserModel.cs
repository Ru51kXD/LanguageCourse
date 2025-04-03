using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication15.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedApplicationUserModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_Tests_TestId",
                table: "TestResults");

            migrationBuilder.DropForeignKey(
                name: "FK_WatchedVideos_Videos_VideoId",
                table: "WatchedVideos");

            migrationBuilder.AddColumn<int>(
                name: "LanguageLevelId1",
                table: "Videos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LanguageLevelId1",
                table: "Tests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Videos_LanguageLevelId1",
                table: "Videos",
                column: "LanguageLevelId1");

            migrationBuilder.CreateIndex(
                name: "IX_Tests_LanguageLevelId1",
                table: "Tests",
                column: "LanguageLevelId1");

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_Tests_TestId",
                table: "TestResults",
                column: "TestId",
                principalTable: "Tests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tests_LanguageLevels_LanguageLevelId1",
                table: "Tests",
                column: "LanguageLevelId1",
                principalTable: "LanguageLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_LanguageLevels_LanguageLevelId1",
                table: "Videos",
                column: "LanguageLevelId1",
                principalTable: "LanguageLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WatchedVideos_Videos_VideoId",
                table: "WatchedVideos",
                column: "VideoId",
                principalTable: "Videos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_Tests_TestId",
                table: "TestResults");

            migrationBuilder.DropForeignKey(
                name: "FK_Tests_LanguageLevels_LanguageLevelId1",
                table: "Tests");

            migrationBuilder.DropForeignKey(
                name: "FK_Videos_LanguageLevels_LanguageLevelId1",
                table: "Videos");

            migrationBuilder.DropForeignKey(
                name: "FK_WatchedVideos_Videos_VideoId",
                table: "WatchedVideos");

            migrationBuilder.DropIndex(
                name: "IX_Videos_LanguageLevelId1",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Tests_LanguageLevelId1",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "LanguageLevelId1",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "LanguageLevelId1",
                table: "Tests");

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_Tests_TestId",
                table: "TestResults",
                column: "TestId",
                principalTable: "Tests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WatchedVideos_Videos_VideoId",
                table: "WatchedVideos",
                column: "VideoId",
                principalTable: "Videos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
