namespace BusinessLogic.Utils.EmailService
{
    public interface IEmailService
    {
        Task SendNotificationAboutDenyCharity(string toEmail, string name, string reason);
        Task SendNotificationForCreatingAccountForBranchAdminEmail(
            string toEmail,
            string userName,
            string phone,
            string password
        );
        Task SendNotificationForCreatingAccountForCharityUnitEmail(
            string toEmail,
            string userName,
            string phone,
            string password
        );
        Task SendNotificationForDenyCharityUnitUpdateEmail(
            string toEmail,
            string name,
            string reason
        );
        Task SendVerificationEmail(string toEmail, string emailVerificationLink);
        Task SendVerifyCodeToEmail(string toEmail, string VerifyCode);
    }
}
