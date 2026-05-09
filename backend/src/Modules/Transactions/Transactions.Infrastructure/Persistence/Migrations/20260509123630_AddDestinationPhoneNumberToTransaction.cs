using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EWallet.Modules.Transactions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDestinationPhoneNumberToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DestinationPhoneNumber",
                schema: "transactions",
                table: "transactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinationPhoneNumber",
                schema: "transactions",
                table: "transactions");
        }
    }
}
