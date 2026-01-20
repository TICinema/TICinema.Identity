using TICinema.Identity.Application.DTOs.Inputs;
using TICinema.Identity.Application.DTOs.Outputs;

namespace TICinema.Identity.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<bool> SendOtpAsync(SendOtpDto dto);
        Task<VerifyOtpResponse> VerifyOtpAsync(VerifyOtpDto dto);
        Task<VerifyOtpResponse> RefreshAsync(string refreshToken);
        Task<bool> AssignRoleAsync(string userId, string roleName);
    }
}
