using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Be_QuanLyKhoaHoc.Migrations
{
    /// <inheritdoc />
    public partial class SeedRolesAndAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "9ad14217-3437-4b7b-b1ce-4543caa29e8e", "f4c44b8c-0fc0-44a5-aa57-b70661b4198f", "Lecturer", "LECTURER" },
                    { "df789f02-0431-4245-87f8-d66f5c6f8456", "36a35e81-9069-4e7e-8013-f4f852ef02e8", "Admin", "ADMIN" },
                    { "fbf1f959-3464-4923-b5b1-b15c0b9d8c4a", "3d3205ff-0400-4d78-9919-c3386357d351", "User", "USER" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "CreatedAt", "Email", "EmailConfirmed", "FullName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UpdatedAt", "UserName" },
                values: new object[] { "2ce6e598-ae70-48e0-a07c-336705250b8e", 0, "1a86c9b5-ab1e-456e-b437-6ebcc84fd39f", new DateTime(2025, 1, 23, 6, 24, 14, 308, DateTimeKind.Utc).AddTicks(9186), "admin@ntt.com", true, "Admin", false, null, "ADMIN@NTT.COM", "ADMIN@NTT.COM", "AQAAAAIAAYagAAAAEDd9SsRZN1ua6rvgt5wSTYVoAykWJkGbm9ui6gYYmYQA2r6Mi73st1bfIncmvIwhfA==", null, false, "c240ea28-1354-4a4c-aab2-685183e92386", false, new DateTime(2025, 1, 23, 6, 24, 14, 308, DateTimeKind.Utc).AddTicks(9189), "admin@ntt.com" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { "9ad14217-3437-4b7b-b1ce-4543caa29e8e", "2ce6e598-ae70-48e0-a07c-336705250b8e" },
                    { "df789f02-0431-4245-87f8-d66f5c6f8456", "2ce6e598-ae70-48e0-a07c-336705250b8e" },
                    { "fbf1f959-3464-4923-b5b1-b15c0b9d8c4a", "2ce6e598-ae70-48e0-a07c-336705250b8e" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "9ad14217-3437-4b7b-b1ce-4543caa29e8e", "2ce6e598-ae70-48e0-a07c-336705250b8e" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "df789f02-0431-4245-87f8-d66f5c6f8456", "2ce6e598-ae70-48e0-a07c-336705250b8e" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "fbf1f959-3464-4923-b5b1-b15c0b9d8c4a", "2ce6e598-ae70-48e0-a07c-336705250b8e" });

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "9ad14217-3437-4b7b-b1ce-4543caa29e8e");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "df789f02-0431-4245-87f8-d66f5c6f8456");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "fbf1f959-3464-4923-b5b1-b15c0b9d8c4a");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "2ce6e598-ae70-48e0-a07c-336705250b8e");
        }
    }
}
