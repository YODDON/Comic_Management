using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletAPI.Migrations
{
    /// <inheritdoc />
    public partial class RequireWalletUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets");

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [dbo].[Wallets] WHERE [UserId] IS NULL)
                    THROW 51001, 'Every wallet must have a valid UserId before it can be required.', 1;

                DECLARE @defaultConstraint sysname;
                SELECT @defaultConstraint = [d].[name]
                FROM [sys].[default_constraints] [d]
                INNER JOIN [sys].[columns] [c]
                    ON [d].[parent_object_id] = [c].[object_id]
                    AND [d].[parent_column_id] = [c].[column_id]
                WHERE [d].[parent_object_id] = OBJECT_ID(N'[dbo].[Wallets]')
                    AND [c].[name] = N'UserId';

                IF @defaultConstraint IS NOT NULL
                    EXEC(N'ALTER TABLE [dbo].[Wallets] DROP CONSTRAINT [' + @defaultConstraint + N']');

                ALTER TABLE [dbo].[Wallets] ALTER COLUMN [UserId] int NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Wallets",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }
    }
}
