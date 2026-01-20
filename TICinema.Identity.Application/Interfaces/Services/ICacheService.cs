namespace TICinema.Identity.Application.Interfaces.Services
{
    public interface ICacheService
    {
        Task SendAsync<T>(string key, T value, TimeSpan? timeExpiration = null);
        Task<T?> GetAsync<T>(string key);
        Task RemoveAsync(string key);
    }
}
