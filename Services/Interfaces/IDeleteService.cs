using SampleProject.Common;

namespace Be_QuanLyKhoaHoc.Services.Interfaces
{
    public interface IDeleteService
    {
        Task<Result<string>> DeleteAssignmentAsync(int assignmentId, string lecturerId);
        Task<Result<string>> DeleteExamAsync(int examId, string lecturerId);
        Task<Result<string>> DeleteLessonAsync(int lessonId, string lecturerId);
        Task<Result<string>> DeleteLessonContentAsync(int lessonContentId);
    }

}
