using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtSnowballApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTempUserIdToQaDebtItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TempUserId",
                table: "QaDebtItems",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TempUserId",
                table: "QaDebtItems");
        }
    }
}
