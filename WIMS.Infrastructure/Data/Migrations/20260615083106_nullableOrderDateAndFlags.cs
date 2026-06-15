using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WIMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class nullableOrderDateAndFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CancellationReason",
                table: "purchase_orders",
                newName: "RejectionReason");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "OrderDate",
                table: "purchase_orders",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectedBy",
                table: "purchase_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_ApprovedBy",
                table: "purchase_orders",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_CancelledBy",
                table: "purchase_orders",
                column: "CancelledBy");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_RejectedBy",
                table: "purchase_orders",
                column: "RejectedBy");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_SubmittedBy",
                table: "purchase_orders",
                column: "SubmittedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_orders_users_ApprovedBy",
                table: "purchase_orders",
                column: "ApprovedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_orders_users_CancelledBy",
                table: "purchase_orders",
                column: "CancelledBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_orders_users_RejectedBy",
                table: "purchase_orders",
                column: "RejectedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_orders_users_SubmittedBy",
                table: "purchase_orders",
                column: "SubmittedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchase_orders_users_ApprovedBy",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_orders_users_CancelledBy",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_orders_users_RejectedBy",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_orders_users_SubmittedBy",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_ApprovedBy",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_CancelledBy",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_RejectedBy",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_SubmittedBy",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "RejectedBy",
                table: "purchase_orders");

            migrationBuilder.RenameColumn(
                name: "RejectionReason",
                table: "purchase_orders",
                newName: "CancellationReason");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "OrderDate",
                table: "purchase_orders",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }
    }
}
