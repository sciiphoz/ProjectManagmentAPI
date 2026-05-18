namespace ProjectManagementAPI.Interfaces
{
    public interface IEmailService
    {
        Task SendPasswordResetCodeAsync(string email, string code);
        Task SendEmailConfirmationAsync(string email, string confirmationLink);
        Task SendProjectInvitationAsync(string email, string projectName, string invitationLink);
    }
}
