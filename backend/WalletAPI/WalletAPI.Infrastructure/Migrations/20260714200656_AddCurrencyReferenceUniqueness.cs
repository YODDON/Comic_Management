using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyReferenceUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CurrencyEntries_ReferenceId",
                table: "CurrencyEntries");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyEntries_ReferenceId",
                table: "CurrencyEntries",
                column: "ReferenceId",
                unique: true,
                filter: "[ReferenceId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CurrencyEntries_ReferenceId",
                table: "CurrencyEntries");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyEntries_ReferenceId",
                table: "CurrencyEntries",
                column: "ReferenceId");
        }
    }
}
