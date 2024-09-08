using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Messaging.Migrations
{
    /// <inheritdoc />
    public partial class EmailSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "ClientRegistrations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                table: "ClientRegistrations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "ClientRegistrations");

            migrationBuilder.DropColumn(
                name: "EmailVerified",
                table: "ClientRegistrations");
        }
    }
}
