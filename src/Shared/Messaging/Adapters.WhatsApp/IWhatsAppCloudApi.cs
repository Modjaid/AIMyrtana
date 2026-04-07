using Messaging.Abstractions;

namespace Adapters.WhatsApp;

/// <summary>
/// Provider-specific HTTP client (Meta Cloud API, Twilio, etc.); implement in product or integration layer.
/// </summary>
public interface IWhatsAppCloudApi
{
    Task<SendResult> SendTextAsync(string toPhoneE164, string text, CancellationToken cancellationToken = default);
}
