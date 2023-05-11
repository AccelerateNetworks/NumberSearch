using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Messaging.Migrations
{
    /// <inheritdoc />
    public partial class RawResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientSecret",
                table: "Messages");

            migrationBuilder.RenameColumn(
                name: "ToFromCompound",
                table: "Messages",
                newName: "RawResponse");

            migrationBuilder.RenameColumn(
                name: "DLRID",
                table: "Messages",
                newName: "RawRequest");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RawResponse",
                table: "Messages",
                newName: "ToFromCompound");

            migrationBuilder.RenameColumn(
                name: "RawRequest",
                table: "Messages",
                newName: "DLRID");

            migrationBuilder.AddColumn<string>(
                name: "ClientSecret",
                table: "Messages",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
