namespace TICinema.Identity.Application.DTOs.Outputs;

public record VerifyOtpResponse(string AccessToken, string RefreshToken);