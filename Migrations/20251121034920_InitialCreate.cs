using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TwitterClone.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    PhotoURL = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CoverPhotoURL = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Verified = table.Column<bool>(type: "bit", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Accent = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Following = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Followers = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalTweets = table.Column<int>(type: "int", nullable: false),
                    TotalPhotos = table.Column<int>(type: "int", nullable: false),
                    PinnedTweet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tweets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(560)", maxLength: 560, nullable: true),
                    Images = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ParentUsername = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserLikes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserRetweets = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserReplies = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tweets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tweets_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserStats",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Likes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tweets = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookmarks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TweetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookmarks_Tweets_TweetId",
                        column: x => x.TweetId,
                        principalTable: "Tweets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Bookmarks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_CreatedAt",
                table: "Bookmarks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_TweetId",
                table: "Bookmarks",
                column: "TweetId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId",
                table: "Bookmarks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId_TweetId",
                table: "Bookmarks",
                columns: new[] { "UserId", "TweetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tweets_CreatedAt",
                table: "Tweets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tweets_CreatedBy",
                table: "Tweets",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tweets_ParentId",
                table: "Tweets",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserStats_UserId",
                table: "UserStats",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookmarks");

            migrationBuilder.DropTable(
                name: "UserStats");

            migrationBuilder.DropTable(
                name: "Tweets");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
