using TICinema.Contracts.Protos.Identity;

namespace TICinema.Identity.Application.Interfaces.Services
{
    public interface ITelegramService
    {
        public string GetAuthUrl();
        Task<TelegramVerifyResponse> VerifyAsync(IDictionary<string, string> query);
        Task<TelegramCompleteResponse> CompleteAsync(TelegramCompleteRequest request);
        Task<TelegramConsumeResponse> ConsumeAsync(TelegramConsumeRequest request);
    }
}
