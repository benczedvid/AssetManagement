using AssetManagement.Application.Common.Email;

namespace AssetManagement.Application.Common.Interfaces.Email
{
    public interface IEmailSender
    {
        Task SendAsync( EmailMessage message, CancellationToken cancellationToken);
    }
}
