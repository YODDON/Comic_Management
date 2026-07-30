using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentAPI.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Transactions] (
                    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Transactions] PRIMARY KEY,
                    [UserId] int NOT NULL, [ChapterId] uniqueidentifier NULL,
                    [Type] nvarchar(30) NOT NULL, [Amount] decimal(18,2) NOT NULL,
                    [CurrencyType] nvarchar(max) NOT NULL, [Status] nvarchar(30) NOT NULL,
                    [Note] nvarchar(1000) NULL, [PaymentMethod] nvarchar(max) NOT NULL,
                    [TransactionCode] nvarchar(450) NOT NULL, [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL
                );
            END
            ELSE
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'[dbo].[Transactions]') AND name=N'UserId' AND system_type_id=36)
                BEGIN
                    EXEC sp_rename N'dbo.Transactions.UserId', N'LegacyUserId', 'COLUMN';
                    ALTER TABLE [dbo].[Transactions] ADD [UserId] int NOT NULL CONSTRAINT [DF_Transactions_UserId] DEFAULT(0) WITH VALUES;
                END;
                IF COL_LENGTH(N'dbo.Transactions', N'ChapterId') IS NULL ALTER TABLE [dbo].[Transactions] ADD [ChapterId] uniqueidentifier NULL;
                IF COL_LENGTH(N'dbo.Transactions', N'Note') IS NULL ALTER TABLE [dbo].[Transactions] ADD [Note] nvarchar(1000) NULL;

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'[dbo].[Transactions]') AND name=N'Type' AND system_type_id=56)
                BEGIN
                    EXEC sp_rename N'dbo.Transactions.Type', N'LegacyType', 'COLUMN';
                    ALTER TABLE [dbo].[Transactions] ADD [Type] nvarchar(30) NOT NULL CONSTRAINT [DF_Transactions_Type] DEFAULT(N'Purchase') WITH VALUES;
                    EXEC(N'UPDATE [dbo].[Transactions] SET [Type]=CASE [LegacyType] WHEN 0 THEN N''Purchase'' WHEN 1 THEN N''ManualTopUp'' WHEN 2 THEN N''Withdraw'' ELSE N''Refund'' END');
                END;
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'[dbo].[Transactions]') AND name=N'Status' AND system_type_id=56)
                BEGIN
                    EXEC sp_rename N'dbo.Transactions.Status', N'LegacyStatus', 'COLUMN';
                    ALTER TABLE [dbo].[Transactions] ADD [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_Transactions_Status] DEFAULT(N'Pending') WITH VALUES;
                    EXEC(N'UPDATE [dbo].[Transactions] SET [Status]=CASE [LegacyStatus] WHEN 2 THEN N''Completed'' WHEN 3 THEN N''Rejected'' ELSE N''Pending'' END');
                END;
            END;

            IF OBJECT_ID(N'[dbo].[UserPurchases]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[UserPurchases] (
                    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_UserPurchases] PRIMARY KEY,
                    [UserId] int NOT NULL, [ComicId] uniqueidentifier NOT NULL,
                    [ChapterId] uniqueidentifier NOT NULL, [Price] decimal(18,2) NOT NULL,
                    [PurchasedAt] datetime2 NOT NULL, [CreatedAt] datetime2 NOT NULL, [UpdatedAt] datetime2 NULL
                );
            END
            ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'[dbo].[UserPurchases]') AND name=N'UserId' AND system_type_id=36)
            BEGIN
                EXEC sp_rename N'dbo.UserPurchases.UserId', N'LegacyUserId', 'COLUMN';
                ALTER TABLE [dbo].[UserPurchases] ADD [UserId] int NOT NULL CONSTRAINT [DF_UserPurchases_UserId] DEFAULT(0) WITH VALUES;
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_UserPurchases_UserId_ChapterId' AND object_id=OBJECT_ID(N'[dbo].[UserPurchases]'))
                CREATE UNIQUE INDEX [IX_UserPurchases_UserId_ChapterId] ON [dbo].[UserPurchases]([UserId],[ChapterId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Transactions_TransactionCode' AND object_id=OBJECT_ID(N'[dbo].[Transactions]'))
            BEGIN
                ALTER TABLE [dbo].[Transactions] ALTER COLUMN [TransactionCode] nvarchar(450) NOT NULL;
                CREATE UNIQUE INDEX [IX_Transactions_TransactionCode] ON [dbo].[Transactions]([TransactionCode]) WHERE [TransactionCode] <> '';
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) { }
}
