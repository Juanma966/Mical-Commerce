using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mical.Migrations
{
    /// <inheritdoc />
    public partial class RemovePromotionLinkFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkText",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "LinkUrl",
                table: "Promotions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkText",
                table: "Promotions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkUrl",
                table: "Promotions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }
    }
}
