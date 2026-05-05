using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EWallet.Modules.Transactions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionAndNotesToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "transactions",
                table: "transactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "transactions",
                table: "transactions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                schema: "transactions",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "transactions",
                table: "transactions");
        }
    }
}
