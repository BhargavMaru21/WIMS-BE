using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WIMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetTokenFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "users");

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiry",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshTokenHash",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierContact",
                table: "purchase_orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiry",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenHash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "RefreshTokenHash",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierContact",
                table: "purchase_orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }
    }
}
