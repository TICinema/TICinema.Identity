using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using TICinema.Identity.Application.Interfaces;
using TICinema.Identity.Application.Interfaces.Services;

namespace TICinema.Identity.Infrastructure.Services
{
    public class RedisCacheService(IDistributedCache cache) : ICacheService
    {
        public async Task SendAsync<T>(string key, T value, TimeSpan? timeExpiration = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = timeExpiration ?? TimeSpan.FromMinutes(5)
            };

            var jsonData = JsonSerializer.Serialize(value);
            await cache.SetStringAsync(key, jsonData, options);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var jsonData = await cache.GetStringAsync(key);
            return jsonData is null ? default : JsonSerializer.Deserialize<T>(jsonData);
        }

        public async Task RemoveAsync(string key) => await cache.RemoveAsync(key); 
    }
}
