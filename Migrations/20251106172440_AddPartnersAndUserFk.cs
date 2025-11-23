using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtSnowballApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnersAndUserFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Partners_PartnerId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Partners_Code",
                table: "Partners",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Partners_PartnerId",
                table: "AspNetUsers",
                column: "PartnerId",
                principalTable: "Partners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Partners_PartnerId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Partners_Code",
                table: "Partners");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Partners_PartnerId",
                table: "AspNetUsers",
                column: "PartnerId",
                principalTable: "Partners",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
