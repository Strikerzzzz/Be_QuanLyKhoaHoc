namespace Be_QuanLyKhoaHoc.Services.Interfaces
{
    public interface IAppEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
