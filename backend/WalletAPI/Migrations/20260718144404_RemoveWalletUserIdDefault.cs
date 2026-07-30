using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWalletUserIdDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Earlier local runs of RequireWalletUserId may have received EF's temporary
            // default(0). A wallet must always be created with an explicit, valid UserId.
            migrationBuilder.Sql("""
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Do not restore a default UserId=0 because it would create invalid wallets.
        }
    }
}
