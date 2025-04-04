using Be_QuanLyKhoaHoc.Identity;
using Be_QuanLyKhoaHoc.Services.Interfaces;
using SampleProject.Common;
using Microsoft.EntityFrameworkCore;
using System;
using Be_QuanLyKhoaHoc.Identity.Entities;

namespace Be_QuanLyKhoaHoc.Services
{
    public class DeleteService : IDeleteService
    {
        private readonly ApplicationDbContext _context;
        private readonly S3Service _s3Service;

        public DeleteService(ApplicationDbContext context, S3Service s3Service)
        {
            _context = context;
            _s3Service = s3Service;
        }

        public async Task<Result<string>> DeleteAssignmentAsync(int assignmentId, string lecturerId)
        {
            try
            {
                var assignmentInfo = await _context.Assignments
                    .Where(a => a.AssignmentId == assignmentId)
                    .Select(a => new
                    {
                        a.AssignmentId,
                        LecturerId = a.Lesson != null && a.Lesson.Course != null ? a.Lesson.Course.LecturerId : null
                    })
                    .FirstOrDefaultAsync();

                if (assignmentInfo == null)
                {
                    return Result<string>.Failure(new[] { "Không tìm thấy bài tập." });
                }

                if (assignmentInfo.LecturerId == null || assignmentInfo.LecturerId != lecturerId)
                {
                    return Result<string>.Failure(new[] { "Bạn không có quyền xóa bài tập này." });
                }

                // Xóa Questions
                await _context.Questions
                    .Where(q => q.AssignmentId == assignmentId)
                    .ExecuteDeleteAsync();

                // Xóa AssignmentResults
                await _context.AssignmentResults
                    .Where(ar => ar.AssignmentId == assignmentId)
                    .ExecuteDeleteAsync();

                // Xóa Assignment
                var deleted = await _context.Assignments
                    .Where(a => a.AssignmentId == assignmentId)
                    .ExecuteDeleteAsync();

                if (deleted > 0)
                {
                    return Result<string>.Success("Xóa bài tập và dữ liệu liên quan thành công!");
                }

                return Result<string>.Failure(new[] { "Xóa bài tập thất bại." });
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        public async Task<Result<string>> DeleteExamAsync(int examId, string lecturerId)
        {
            try
            {
                var exam = await _context.Exams
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.ExamId == examId);

                if (exam == null)
                {
                    return Result<string>.Failure(new[] { "Không tìm thấy bài kiểm tra." });
                }

                if (exam.Course?.LecturerId == null || exam.Course.LecturerId != lecturerId)
                {
                    return Result<string>.Failure(new[] { "Bạn không có quyền xóa bài kiểm tra này." });
                }

                // Xóa Questions liên quan
                var questions = await _context.Questions
                    .Where(q => q.ExamId == examId)
                    .ToListAsync();
                _context.Questions.RemoveRange(questions);

                // Xóa ExamResults
                var examResults = await _context.ExamResults
                    .Where(er => er.ExamId == examId)
                    .ToListAsync();
                _context.ExamResults.RemoveRange(examResults);

                // Xóa Exam
                _context.Exams.Remove(exam);

                await _context.SaveChangesAsync();

                return Result<string>.Success("Xóa bài kiểm tra và dữ liệu liên quan thành công!");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" });
            }
        }
        public async Task<Result<string>> DeleteLessonAsync(int lessonId, string lecturerId)
        {
            try
            {
                var lessonInfo = await _context.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.LessonId == lessonId);

                if (lessonInfo == null)
                {
                    return Result<string>.Failure(new[] { "Không tìm thấy bài học." });
                }

                if (lessonInfo.Course?.LecturerId != lecturerId)
                {
                    return Result<string>.Failure(new[] { "Bạn không có quyền xóa bài học này." });
                }

                // Step 1: Xóa LessonContents dùng service
                var lessonContentIds = await _context.LessonContents
                    .Where(lc => lc.LessonId == lessonId)
                    .Select(lc => lc.LessonContentId)
                    .ToListAsync();

                foreach (var contentId in lessonContentIds)
                {
                    var result = await DeleteLessonContentAsync(contentId);
                    if (!result.Succeeded)
                    {
                        return Result<string>.Failure(new[]
                        {
                            $"Xóa bài học thất bại trong lúc xóa nội dung bài học (ID: {contentId}): {result.Errors?.FirstOrDefault()}"
                        });
                    }
                }

                // Step 2: Xóa CompletedLessons
                await _context.CompletedLessons
                    .Where(cl => cl.LessonId == lessonId)
                    .ExecuteDeleteAsync();

                // Step 3: Xử lý các Assignment liên quan
                var assignmentIds = await _context.Assignments
                    .Where(a => a.LessonId == lessonId)
                    .Select(a => a.AssignmentId)
                    .ToListAsync();

                foreach (var assignmentId in assignmentIds)
                {
                    var deleteResult = await DeleteAssignmentAsync(assignmentId, lecturerId);
                    if (!deleteResult.Succeeded)
                    {
                        return Result<string>.Failure(new[] {
                    $"Xóa bài học thất bại trong lúc xóa bài tập (ID: {assignmentId}): {deleteResult.Errors?.FirstOrDefault()}"
                });
                    }
                }

                // Step 4: Xóa Lesson
                var deleted = await _context.Lessons
                    .Where(l => l.LessonId == lessonId)
                    .ExecuteDeleteAsync();

                if (deleted > 0)
                {
                    return Result<string>.Success("Xóa bài học và các dữ liệu liên quan thành công!");
                }

                return Result<string>.Failure(new[] { "Xóa bài học thất bại." });
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" });
            }
        }


        public async Task<Result<string>> DeleteLessonContentAsync(int id)
        {
            try
            {
                var lessonContent = await _context.LessonContents
                    .FirstOrDefaultAsync(lc => lc.LessonContentId == id); // KHÔNG dùng AsNoTracking

                if (lessonContent == null)
                {
                    return Result<string>.Failure(new[] { "Không tìm thấy nội dung bài học." });
                }

                if (!string.IsNullOrEmpty(lessonContent.MediaUrl))
                {
                    string objectKey = GetObjectKey(lessonContent.MediaType, lessonContent.MediaUrl);

                    if (!string.IsNullOrEmpty(objectKey))
                    {
                        if (lessonContent.MediaType == "video" && objectKey.EndsWith(".m3u8"))
                        {
                            string directoryKey = objectKey.Substring(0, objectKey.LastIndexOf('/') + 1);
                            await _s3Service.DeleteS3DirectoryAsync(directoryKey);
                        }
                        else
                        {
                            await _s3Service.DeleteS3ObjectAsync(objectKey);
                        }
                    }
                }

                _context.LessonContents.Remove(lessonContent);
                await _context.SaveChangesAsync();

                return Result<string>.Success("Xóa nội dung bài học thành công.");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" });
            }
        }


        public string GetObjectKey(string mediaType, string mediaUrl)
        {
            if (mediaType == "image")
            {
                return ExtractS3ObjectKey(mediaUrl);
            }
            else if (mediaType == "video")
            {
                return mediaUrl;
            }
            return string.Empty;
        }

        private string ExtractS3ObjectKey(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                string path = uri.AbsolutePath;
                return string.IsNullOrEmpty(path) || path == "/" ? string.Empty : path.TrimStart('/');
            }
            catch
            {
                return string.Empty;
            }
        }

    }
}
