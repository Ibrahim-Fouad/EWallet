using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EWallet.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixOpenIddictTokenTypeLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // OpenIddict 5.x configures OpenIddictTokens.Type as nvarchar(50) by default.
            // The authorization_code token type URI is 57 characters, which truncates.
            // Widen to nvarchar(256) to accommodate all current and future token type URIs.
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                schema: "identity",
                table: "OpenIddictTokens",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                schema: "identity",
                table: "OpenIddictTokens",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);
        }
    }
}
