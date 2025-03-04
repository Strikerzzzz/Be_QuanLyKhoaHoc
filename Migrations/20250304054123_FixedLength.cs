using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Be_QuanLyKhoaHoc.Migrations
{
    /// <inheritdoc />
    public partial class FixedLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MediaUrl",
                table: "LessonContents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a0d9ec33-7b25-4f40-9f4f-1e4d0f2e9842",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "49a6b68d-63ef-4447-a6b4-993d40ea7626", new DateTime(2025, 3, 4, 5, 41, 21, 0, DateTimeKind.Utc).AddTicks(2251), "AQAAAAIAAYagAAAAEAl+vbNiHDHxLopxLRTPFurIadFY1FxMxIHIBjbhYhu9nHEOMsNBgvDw9RP9j//ZcA==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MediaUrl",
                table: "LessonContents",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a0d9ec33-7b25-4f40-9f4f-1e4d0f2e9842",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "908a1854-30cd-4996-a42d-7a627c01dc69", new DateTime(2025, 2, 27, 16, 12, 51, 122, DateTimeKind.Utc).AddTicks(8888), "AQAAAAIAAYagAAAAEByJ0d+P87ZxmbOkspwVmWKZX/n0V2FHJP0Qn2LxhIy1SgCUNdMclHzjgc9/Vt6bKA==" });
        }
    }
}
