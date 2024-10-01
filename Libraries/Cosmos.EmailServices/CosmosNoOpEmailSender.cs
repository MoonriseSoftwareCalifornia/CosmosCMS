namespace Cosmos.EmailServices
{
    /// <summary>
    /// NoOp Email Sender.
    /// </summary>
    internal class CosmosNoOpEmailSender : ICosmosEmailSender
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosNoOpEmailSender"/> class.
        /// </summary>
        public CosmosNoOpEmailSender()
        {
        }

        /// <inheritdoc/>
        public SendResult SendResult => new SendResult() { Message = "NoOpEmailSender", StatusCode = System.Net.HttpStatusCode.OK };

        /// <inheritdoc/>
        public Task SendEmailAsync(string emailTo, string subject, string textVersion, string htmlVersion, string? emailFrom = null)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return Task.CompletedTask;
        }
    }
}
