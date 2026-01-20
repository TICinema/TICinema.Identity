using Grpc.Core;
using TICinema.Contracts.Protos.Identity;
using TICinema.Identity.Application.DTOs.Inputs;
using TICinema.Identity.Application.Interfaces;
using TICinema.Identity.Application.Interfaces.Services;

namespace TICinema.Identity.Grpc.Services;

public class AuthService(ILogger<AuthService> logger, IAuthService authService) 
    : Contracts.Protos.Identity.AuthService.AuthServiceBase
{
    public override async Task<SendOtpResponse> SendOtp(SendOtpRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new SendOtpDto(request.Identifier, request.Type);
            var result = await authService.SendOtpAsync(dto);
            
            return new SendOtpResponse { Ok = result };
        }
        catch (ArgumentException ex)
        {
            // Аналог BadRequest — неверные входные данные
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при отправке OTP для {Identifier}", request.Identifier);
            throw new RpcException(new Status(StatusCode.Internal, "Внутренняя ошибка сервера"));
        }
    }

    public override async Task<VerifyOtpResponse> VerifyOtp(VerifyOtpRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new VerifyOtpDto(request.Identifier, request.Type, request.Code);
            var result = await authService.VerifyOtpAsync(dto);

            return new VerifyOtpResponse 
            { 
                AccessToken = result.AccessToken, 
                RefreshToken = result.RefreshToken 
            };
        }
        catch (Exception ex) when (ex.Message.Contains("Неверный код") || ex.Message.Contains("истек"))
        {
            // Меняем Unauthenticated на InvalidArgument (Код 400)
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка верификации");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<RefreshResponse> Refresh(RefreshRequest request, ServerCallContext context)
    {
        try
        {
            // Вызываем сервис
            var result = await authService.RefreshAsync(request.RefreshToken);

            return new RefreshResponse 
            { 
                AccessToken = result.AccessToken, 
                RefreshToken = result.RefreshToken 
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning("Попытка обновления токена не удалась: {Message}", ex.Message);
            // Возвращаем Unauthenticated, чтобы Gateway понял, что куку надо очистить
            throw new RpcException(new Status(StatusCode.Unauthenticated, ex.Message));
        }
    }
}