using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Be_QuanLyKhoaHoc.Migrations
{
    /// <inheritdoc />
    public partial class FixedQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnswerGroupNumber",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RandomMultipleChoiceCount",
                table: "Exams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RandomMultipleChoiceCount",
                table: "Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a0d9ec33-7b25-4f40-9f4f-1e4d0f2e9842",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "55d10222-6daf-4c6f-918b-724c0833b06b", new DateTime(2025, 2, 21, 7, 21, 38, 600, DateTimeKind.Utc).AddTicks(4774), "AQAAAAIAAYagAAAAEO/VzSAQ03mBjuacL/9IdWI6rlSyGVib495aDJXPDqYeSFPaIhIAgAVf1i+JrrP62w==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnswerGroupNumber",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "RandomMultipleChoiceCount",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "RandomMultipleChoiceCount",
                table: "Assignments");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a0d9ec33-7b25-4f40-9f4f-1e4d0f2e9842",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "3761bb05-7287-4966-a7e2-e0d0aef4a049", new DateTime(2025, 2, 14, 5, 58, 9, 609, DateTimeKind.Utc).AddTicks(8781), "AQAAAAIAAYagAAAAENyhpnnKbUgaewWiu9sFLeZQtAOPDaAjCDj0P/CyzmM1v9klNZgsd7H72fE/ZeUGHw==" });
        }
    }
}
