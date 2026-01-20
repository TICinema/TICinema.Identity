using Microsoft.AspNetCore.Mvc;
using TICinema.Identity.Application.Interfaces;
using TICinema.Identity.Application.Interfaces.Services;

[ApiController]
[Route("internal/setup")]
public class SetupController : ControllerBase
{
    private readonly IAuthService _authService;
    private const string AdminSecret = "Super-Secret-Key-123"; // В идеале вынести в .env

    public SetupController(IAuthService authService) => _authService = authService;

    [HttpPost("promote")]
    public async Task<IActionResult> PromoteToAdmin([FromQuery] string userId, [FromHeader(Name = "X-Admin-Key")] string key)
    {
        // Простая защита, чтобы никто не стал админом без ключа
        if (key != AdminSecret) return Unauthorized("Неверный секретный ключ");

        var result = await _authService.AssignRoleAsync(userId, "Admin");
        
        return result ? Ok($"Пользователь {userId} теперь Admin") : BadRequest("Ошибка при назначении роли");
    }
}