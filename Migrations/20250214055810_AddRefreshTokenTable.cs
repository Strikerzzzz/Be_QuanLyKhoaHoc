using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Be_QuanLyKhoaHoc.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Expires = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Revoked = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a0d9ec33-7b25-4f40-9f4f-1e4d0f2e9842",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "3761bb05-7287-4966-a7e2-e0d0aef4a049", new DateTime(2025, 2, 14, 5, 58, 9, 609, DateTimeKind.Utc).AddTicks(8781), "AQAAAAIAAYagAAAAENyhpnnKbUgaewWiu9sFLeZQtAOPDaAjCDj0P/CyzmM1v9klNZgsd7H72fE/ZeUGHw==" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a0d9ec33-7b25-4f40-9f4f-1e4d0f2e9842",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "ff2dc180-1da5-47b6-9911-0ff2812597f9", new DateTime(2025, 2, 10, 16, 45, 29, 909, DateTimeKind.Utc).AddTicks(9571), "AQAAAAIAAYagAAAAEEr52sH41BXI5uOK00BwMshuKFRP8D0V5KKy3YzCvDb2CSDTmWTVbae5C2zhABfpWA==" });
        }
    }
}
