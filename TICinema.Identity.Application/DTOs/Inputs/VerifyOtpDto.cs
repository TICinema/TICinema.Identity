namespace TICinema.Identity.Application.DTOs.Inputs;

public record VerifyOtpDto(string Identifier, string Type, string Code);