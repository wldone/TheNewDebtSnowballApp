using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtSnowballApp.Migrations
{
    /// <inheritdoc />
    public partial class AddQaDebtItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QaDebtItems_DebtTypes_DebtTypeId",
                table: "QaDebtItems");

            migrationBuilder.DropForeignKey(
                name: "FK_QaDebtItems_Partners_PartnerId",
                table: "QaDebtItems");

            migrationBuilder.DropIndex(
                name: "IX_QaDebtItems_DebtTypeId",
                table: "QaDebtItems");

            migrationBuilder.DropIndex(
                name: "IX_QaDebtItems_PartnerId",
                table: "QaDebtItems");

            migrationBuilder.DropColumn(
                name: "BegBalance",
                table: "QaDebtItems");

            migrationBuilder.DropColumn(
                name: "CreationDate",
                table: "QaDebtItems");

            migrationBuilder.DropColumn(
                name: "DebtTypeId",
                table: "QaDebtItems");

            migrationBuilder.DropColumn(
                name: "LastUpdate",
                table: "QaDebtItems");

            migrationBuilder.DropColumn(
                name: "PartnerId",
                table: "QaDebtItems");

            migrationBuilder.DropColumn(
                name: "Payment",
                table: "QaDebtItems");

            migrationBuilder.DropColumn(
                name: "Rate",
                table: "QaDebtItems");

            migrationBuilder.DropColumn(
                name: "Term",
                table: "QaDebtItems");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "QaDebtItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "InterestRate",
                table: "QaDebtItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "QaDebtItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "QaDebtItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<decimal>(
                name: "InterestRate",
                table: "QaDebtItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "QaDebtItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "BegBalance",
                table: "QaDebtItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreationDate",
                table: "QaDebtItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DebtTypeId",
                table: "QaDebtItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdate",
                table: "QaDebtItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "PartnerId",
                table: "QaDebtItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Payment",
                table: "QaDebtItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Rate",
                table: "QaDebtItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Term",
                table: "QaDebtItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_QaDebtItems_DebtTypeId",
                table: "QaDebtItems",
                column: "DebtTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_QaDebtItems_PartnerId",
                table: "QaDebtItems",
                column: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_QaDebtItems_DebtTypes_DebtTypeId",
                table: "QaDebtItems",
                column: "DebtTypeId",
                principalTable: "DebtTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QaDebtItems_Partners_PartnerId",
                table: "QaDebtItems",
                column: "PartnerId",
                principalTable: "Partners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
