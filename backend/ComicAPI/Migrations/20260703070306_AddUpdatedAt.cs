using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComicAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(8975), new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(8975) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(8985), new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(8986) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(8991), new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(8992) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(8996), new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(8997) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(9004), new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(9004) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(9010), new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(9010) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(9015), new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(9016) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(9031), new DateTime(2026, 7, 3, 7, 3, 4, 565, DateTimeKind.Utc).AddTicks(9031) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4215), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4216) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4222), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4222) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4225), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4225) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4227), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4228) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4230), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4230) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4232), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4233) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4235), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4236) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4243), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4244) });
        }
    }
}
