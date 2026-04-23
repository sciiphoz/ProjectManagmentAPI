namespace ProjectManagementAPI.Interfaces
{
    public interface IEmailService
    {
        Task SendPasswordResetCodeAsync(string email, string code);
        Task SendEmailConfirmationAsync(string email, string confirmationLink);
    }
}
