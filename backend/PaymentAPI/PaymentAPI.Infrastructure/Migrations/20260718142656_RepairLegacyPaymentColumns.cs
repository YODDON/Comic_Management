using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentAPI.Migrations
{
    /// <inheritdoc />
    public partial class RepairLegacyPaymentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // InitialCreate keeps renamed columns when upgrading the legacy Guid/enum schema.
            // Renaming preserves NOT NULL, but the current EF model no longer writes those
            // columns, so every new transaction/purchase would fail at the database boundary.
            // Keep the historical values while allowing rows created by the current model.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.Transactions', N'LegacyUserId') IS NOT NULL
                    ALTER TABLE [dbo].[Transactions] ALTER COLUMN [LegacyUserId] uniqueidentifier NULL;

                IF COL_LENGTH(N'dbo.Transactions', N'LegacyType') IS NOT NULL
                    ALTER TABLE [dbo].[Transactions] ALTER COLUMN [LegacyType] int NULL;

                IF COL_LENGTH(N'dbo.Transactions', N'LegacyStatus') IS NOT NULL
                    ALTER TABLE [dbo].[Transactions] ALTER COLUMN [LegacyStatus] int NULL;

                IF COL_LENGTH(N'dbo.UserPurchases', N'LegacyUserId') IS NOT NULL
                    ALTER TABLE [dbo].[UserPurchases] ALTER COLUMN [LegacyUserId] uniqueidentifier NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This schema repair is intentionally not reversed. Rows created after it have no
            // legacy identifiers/enums, so restoring NOT NULL would either fail or require
            // fabricating historical data.
        }
    }
}
