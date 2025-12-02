using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementAndControl.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddUsernameAndExtraToUserConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "UserConnections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Extra",
                table: "UserConnections",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Username",
                table: "UserConnections");

            migrationBuilder.DropColumn(
                name: "Extra",
                table: "UserConnections");
        }
    }
}
