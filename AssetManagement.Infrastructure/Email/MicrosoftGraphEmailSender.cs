using AssetManagement.Application.Common.Email;
using AssetManagement.Application.Common.Interfaces.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;

namespace AssetManagement.Infrastructure.Email
{
    public sealed class MicrosoftGraphEmailSender :IEmailSender
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly MicrosoftGraphEmailOptions _options;
        private readonly ILogger<MicrosoftGraphEmailSender> _logger;

        public MicrosoftGraphEmailSender(
            GraphServiceClient graphServiceClient,
            IOptions<MicrosoftGraphEmailOptions> options,
            ILogger<MicrosoftGraphEmailSender> logger)
        {

            ArgumentNullException.ThrowIfNull(graphServiceClient);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            _graphServiceClient = graphServiceClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);
            if(message.ToRecipients.Count == 0)
            {
                throw new InvalidOperationException("The email must contain at least one recipientt.");
            }
            if (string.IsNullOrWhiteSpace(message.Subject))
            {
                throw new InvalidOperationException("THe email subject is required.");
            }
            if (string.IsNullOrWhiteSpace(_options.SendUserPrincipalName))
            {
                throw new InvalidOperationException("The MicrosoftGraph sender user principal name is not configured.");
            }
            var recipients = message.ToRecipients
                .Where(recipient => !string.IsNullOrWhiteSpace(recipient))
                .Select(recipient => new Recipient { EmailAddress = new EmailAddress { Address = recipient.Trim() } })
                .ToList();

            if (recipients.Count == 0)
            {
                throw new InvalidOperationException("The email does not contain a valid recipient.");
            }

            var graphMessage = new Message
            {
                Subject = message.Subject,
                Body = new ItemBody { ContentType = BodyType.Html, Content = message.HtmlBody },
                ToRecipients = recipients
            };
            var requestBody = new SendMailPostRequestBody
            {
                Message = graphMessage,
                SaveToSentItems = true
            };
            try
            {
                await _graphServiceClient.Users
                    [
                        _options.SendUserPrincipalName
                    ]
                    .SendMail
                    .PostAsync(requestBody, cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "The email was accepted by Microsoft Graph. " +
                    "Sender: {Sender}, " +
                    "RecipientCount: {RecipientCount}, " +
                    "Subject: {Subject}",
                    _options.SendUserPrincipalName,
                    recipients.Count,
                    message.Subject);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "The email sending operation was cancelled. " +
                    "Sender: {Sender}, " +
                    "Subject: {Subject}",
                    _options.SendUserPrincipalName,
                    message.Subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Microsoft Graph could not send the email. " +
                    "Sender: {Sender}, " +
                    "RecipientCount: {RecipientCount}, " +
                    "Subject: {Subject}",
                    _options.SendUserPrincipalName,
                    recipients.Count,
                    message.Subject);
                throw;
            }
        }
    }
}
