using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Messaging.Migrations
{
    /// <inheritdoc />
    public partial class AddDLRIDColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DateRecievedUTC",
                table: "Messages",
                newName: "DateReceivedUTC");

            migrationBuilder.AddColumn<string>(
                name: "DLRID",
                table: "Messages",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DLRID",
                table: "Messages");

            migrationBuilder.RenameColumn(
                name: "DateReceivedUTC",
                table: "Messages",
                newName: "DateRecievedUTC");
        }
    }
}
