using Messaging.Abstractions;

namespace Adapters.Sms;

public interface ISmsGateway
{
    Task<SendResult> SendAsync(string toPhoneE164, string text, CancellationToken cancellationToken = default);
}
