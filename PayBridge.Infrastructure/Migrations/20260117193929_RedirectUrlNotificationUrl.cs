using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RedirectUrlNotificationUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CallbackUrl",
                table: "Payments",
                newName: "RedirectUrl");

            migrationBuilder.AddColumn<string>(
                name: "NotificationUrl",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationUrl",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "RedirectUrl",
                table: "Payments",
                newName: "CallbackUrl");
        }
    }
}
