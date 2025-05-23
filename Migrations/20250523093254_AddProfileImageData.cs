using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solivio.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileImageData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileImageContentType",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ProfileImageData",
                table: "Users",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImageContentType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfileImageData",
                table: "Users");
        }
    }
}
