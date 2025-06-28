using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddBookReviewmodelandfixrelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLiked",
                table: "BookReviews");

            migrationBuilder.AddColumn<bool>(
                name: "IsLike",
                table: "BookReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLike",
                table: "BookReviews");

            migrationBuilder.AddColumn<bool>(
                name: "IsLiked",
                table: "BookReviews",
                type: "bit",
                nullable: true);
        }
    }
}
