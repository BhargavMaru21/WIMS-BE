using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WIMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAndStatusRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "zones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "zones",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "zones",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "warehouses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "warehouses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "warehouses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "product_categories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "product_categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "product_categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "bins",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "bins",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "bins",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "zones");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "zones");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "zones");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "users");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "users");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "products");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "products");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "products");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "product_categories");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "product_categories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "product_categories");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "bins");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "bins");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "bins");
        }
    }
}
