using System.Threading.Tasks;

namespace LegalCaseAPI.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendTwoFactorCodeAsync(string to, string code);
    Task SendPasswordResetAsync(string to, string token);
}
