using Be_QuanLyKhoaHoc.Identity.Entities;
using Be_QuanLyKhoaHoc.Identity;
using Microsoft.EntityFrameworkCore;

namespace Be_QuanLyKhoaHoc.Services
{
    public class QuestionService
    {
        private readonly ApplicationDbContext _context;

        public QuestionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CheckEntityExists<T>(int entityId, string keyProperty) where T : class
        {
            var entitySet = _context.Set<T>();
            if (entitySet == null)
            {
                throw new InvalidOperationException($"Entity type {typeof(T).Name} không tồn tại trong DbContext.");
            }
            return await entitySet.AsNoTracking().AnyAsync(e => EF.Property<int>(e, keyProperty) == entityId);
        }

        public async Task<List<object>> GetMultipleChoiceQuestions(int entityId, string entityKey)
        {
            return await _context.Questions
                .OfType<MultipleChoiceQuestion>()
                .Where(q => EF.Property<int>(q, entityKey) == entityId)
                .OrderBy(q => q.CreatedAt)
                .Select(q => new
                {
                    q.Id,
                    q.Content,
                    q.Type,
                    q.CreatedAt,
                    Choices = q.Choices,
                    CorrectAnswerIndex = (int?)q.CorrectAnswerIndex,
                    CorrectAnswer = (string)null
                })
                .ToListAsync<object>();
        }

        public async Task<List<object>> GetFillInBlankQuestions(int entityId, string entityKey)
        {
            return await _context.Questions
                .OfType<FillInBlankQuestion>()
                .Where(q => EF.Property<int>(q, entityKey) == entityId)
                .OrderBy(q => q.CreatedAt)
                .Select(q => new
                {
                    q.Id,
                    q.Content,
                    q.Type,
                    q.CreatedAt,
                    Choices = (string)null,
                    CorrectAnswerIndex = (int?)null,
                    CorrectAnswer = q.CorrectAnswer
                })
                .ToListAsync<object>();
        }
    }
}
