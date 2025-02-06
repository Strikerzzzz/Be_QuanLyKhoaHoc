using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Be_QuanLyKhoaHoc.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAddCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    CourseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<double>(type: "float", nullable: true),
                    Difficulty = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Keywords = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LecturerId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.CourseId);
                    table.CheckConstraint("CK_Course_Price", "[Price] >= 0");
                    table.ForeignKey(
                        name: "FK_Courses_AspNetUsers_LecturerId",
                        column: x => x.LecturerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    ExamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.ExamId);
                    table.ForeignKey(
                        name: "FK_Exams_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    LessonId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.LessonId);
                    table.ForeignKey(
                        name: "FK_Lessons_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Progresses",
                columns: table => new
                {
                    ProgressId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    CompletedLessons = table.Column<int>(type: "int", nullable: false),
                    CompletionRate = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAssignmentScore = table.Column<float>(type: "real", nullable: false),
                    TotalExamScore = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Progresses", x => x.ProgressId);
                    table.CheckConstraint("CK_Progress_CompletionRate", "[CompletionRate] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_Progress_TotalScores", "[TotalAssignmentScore] >= 0 AND [TotalExamScore] >= 0");
                    table.ForeignKey(
                        name: "FK_Progresses_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Progresses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamResults",
                columns: table => new
                {
                    ResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<float>(type: "real", nullable: false),
                    SubmissionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamResults", x => x.ResultId);
                    table.ForeignKey(
                        name: "FK_ExamResults_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamResults_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "ExamId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LessonId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignmentType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_Assignments_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "LessonId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LessonContents",
                columns: table => new
                {
                    LessonContentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LessonId = table.Column<int>(type: "int", nullable: false),
                    MediaType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MediaUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonContents", x => x.LessonContentId);
                    table.ForeignKey(
                        name: "FK_LessonContents_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "LessonId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentResults",
                columns: table => new
                {
                    ResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<float>(type: "real", nullable: false),
                    SubmissionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentResults", x => x.ResultId);
                    table.CheckConstraint("CK_AssignmentResult_Score", "[Score] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_AssignmentResults_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentResults_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: false),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FillInBlankQuestion_AssignmentId = table.Column<int>(type: "int", nullable: true),
                    FillInBlankQuestion_ExamId = table.Column<int>(type: "int", nullable: true),
                    Choices = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrectAnswerIndex = table.Column<int>(type: "int", nullable: true),
                    Difficulty = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssignmentId = table.Column<int>(type: "int", nullable: true),
                    ExamId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.CheckConstraint("CK_MultipleChoiceQuestion_CorrectIndex", "[CorrectAnswerIndex] >= 0");
                    table.ForeignKey(
                        name: "FK_Questions_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId");
                    table.ForeignKey(
                        name: "FK_Questions_Assignments_FillInBlankQuestion_AssignmentId",
                        column: x => x.FillInBlankQuestion_AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId");
                    table.ForeignKey(
                        name: "FK_Questions_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "ExamId");
                    table.ForeignKey(
                        name: "FK_Questions_Exams_FillInBlankQuestion_ExamId",
                        column: x => x.FillInBlankQuestion_ExamId,
                        principalTable: "Exams",
                        principalColumn: "ExamId");
                });

            migrationBuilder.CreateTable(
                name: "Answers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceValue = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: true),
                    AssignmentId = table.Column<int>(type: "int", nullable: true),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    UserAnswerIndex = table.Column<int>(type: "int", nullable: true),
                    UserAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Answers_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId");
                    table.ForeignKey(
                        name: "FK_Answers_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "ExamId");
                    table.ForeignKey(
                        name: "FK_Answers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "b6b245a8-0a9d-4d1e-9c26-6bcbf27ef98a", "a1a1a1a1-a1a1-4a1a-a1a1-a1a1a1a1a1a1", "Admin", "ADMIN" },
                    { "c2b5a01e-6b30-4b7b-b450-27b9b39d31f7", "b2b2b2b2-b2b2-4b2b-b2b2-b2b2b2b2b2b2", "Lecturer", "LECTURER" },
                    { "d0a3e13b-8659-4e70-a07e-9a8e2c5a7c45", "c3c3c3c3-c3c3-4c3c-c3c3-c3c3c3c3c3c3", "User", "USER" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "AvatarUrl", "ConcurrencyStamp", "CreatedAt", "Email", "EmailConfirmed", "FullName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "a0d9ec33-7b25-4f40-9f4f-1e4d0f2e9842", 0, null, "30ee71ba-6b4d-4390-ac15-a93c80221440", new DateTime(2025, 2, 5, 13, 46, 4, 3, DateTimeKind.Utc).AddTicks(2768), "admin@ntt.com", true, "Admin", false, null, "ADMIN@NTT.COM", "ADMIN", "AQAAAAIAAYagAAAAELYNLxsnH7ucCXgSd8q9IT8aCgTF1IJ/eSR19foxLvv2hFScemfjuruTycqD1+qXeQ==", null, false, "d4d4d4d4-d4d4-4d4d-d4d4-d4d4d4d4d4d4", false, "admin" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { "b6b245a8-0a9d-4d1e-9c26-6bcbf27ef98a", "a0d9ec33-7b25-4f40-9f4f-1e4d0f2e9842" },
                    { "c2b5a01e-6b30-4b7b-b450-27b9b39d31f7", "a0d9ec33-7b25-4f40-9f4f-1e4d0f2e9842" },
                    { "d0a3e13b-8659-4e70-a07e-9a8e2c5a7c45", "a0d9ec33-7b25-4f40-9f4f-1e4d0f2e9842" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Answers_AssignmentId",
                table: "Answers",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_ExamId",
                table: "Answers",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionId",
                table: "Answers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentResults_AssignmentId",
                table: "AssignmentResults",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentResults_StudentId",
                table: "AssignmentResults",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_LessonId",
                table: "Assignments",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_LecturerId",
                table: "Courses",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamResults_ExamId",
                table: "ExamResults",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamResults_StudentId",
                table: "ExamResults",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_CourseId",
                table: "Exams",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonContents_LessonId",
                table: "LessonContents",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CourseId",
                table: "Lessons",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Progresses_CourseId",
                table: "Progresses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Progresses_StudentId",
                table: "Progresses",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_AssignmentId",
                table: "Questions",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ExamId",
                table: "Questions",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_FillInBlankQuestion_AssignmentId",
                table: "Questions",
                column: "FillInBlankQuestion_AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_FillInBlankQuestion_ExamId",
                table: "Questions",
                column: "FillInBlankQuestion_ExamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Answers");

            migrationBuilder.DropTable(
                name: "AssignmentResults");

            migrationBuilder.DropTable(
                name: "ExamResults");

            migrationBuilder.DropTable(
                name: "LessonContents");

            migrationBuilder.DropTable(
                name: "Progresses");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "Exams");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "b6b245a8-0a9d-4d1e-9c26-6bcbf27ef98a", "a0d9ec33-7b25-4f40-9f4f-1e4d0f2e9842" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "c2b5a01e-6b30-4b7b-b450-27b9b39d31f7", "a0d9ec33-7b25-4f40-9f4f-1e4d0f2e9842" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "d0a3e13b-8659-4e70-a07e-9a8e2c5a7c45", "a0d9ec33-7b25-4f40-9f4f-1e4d0f2e9842" });

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "b6b245a8-0a9d-4d1e-9c26-6bcbf27ef98a");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "c2b5a01e-6b30-4b7b-b450-27b9b39d31f7");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d0a3e13b-8659-4e70-a07e-9a8e2c5a7c45");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a0d9ec33-7b25-4f40-9f4f-1e4d0f2e9842");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

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
    }
}
