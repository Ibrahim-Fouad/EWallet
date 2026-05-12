using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EWallet.Modules.Notifications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentRequestNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TransactionId",
                schema: "notifications",
                table: "notifications",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "ActionStatus",
                schema: "notifications",
                table: "notifications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ActionTakenAt",
                schema: "notifications",
                table: "notifications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                schema: "notifications",
                table: "notifications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MerchantName",
                schema: "notifications",
                table: "notifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentRequestId",
                schema: "notifications",
                table: "notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "notifications",
                table: "notifications",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_PaymentRequestId",
                schema: "notifications",
                table: "notifications",
                columns: new[] { "UserId", "PaymentRequestId" },
                filter: "[PaymentRequestId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_UserId_PaymentRequestId",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "ActionStatus",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "ActionTakenAt",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "MerchantName",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "PaymentRequestId",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.AlterColumn<Guid>(
                name: "TransactionId",
                schema: "notifications",
                table: "notifications",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
