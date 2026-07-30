using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ComicAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalaryType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComicCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComicCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComicCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComicCategories_Comics_ComicId",
                        column: x => x.ComicId,
                        principalTable: "Comics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Outstandings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outstandings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Outstandings_Comics_ComicId",
                        column: x => x.ComicId,
                        principalTable: "Comics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "Name", "Slug", "Tag", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4215), "Action", "action", "Action-packed comics", new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4216) },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4222), "Romance", "romance", "Romantic stories", new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4222) },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4225), "Comedy", "comedy", "Funny and hilarious", new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4225) },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4227), "Fantasy", "fantasy", "Magical worlds", new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4228) },
                    { new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4230), "Horror", "horror", "Scary and thrilling", new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4230) },
                    { new Guid("66666666-6666-6666-6666-666666666666"), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4232), "Sci-Fi", "sci-fi", "Science fiction", new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4233) },
                    { new Guid("77777777-7777-7777-7777-777777777777"), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4235), "Slice of Life", "slice-of-life", "Everyday life", new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4236) },
                    { new Guid("88888888-8888-8888-8888-888888888888"), new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4243), "Drama", "drama", "Emotional and dramatic", new DateTime(2026, 7, 2, 13, 22, 7, 171, DateTimeKind.Utc).AddTicks(4244) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComicCategories_CategoryId",
                table: "ComicCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ComicCategories_ComicId",
                table: "ComicCategories",
                column: "ComicId");

            migrationBuilder.CreateIndex(
                name: "IX_Outstandings_ComicId",
                table: "Outstandings",
                column: "ComicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComicCategories");

            migrationBuilder.DropTable(
                name: "Outstandings");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Comics");
        }
    }
}
