using System.Security.Cryptography;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Options;
using TICinema.Contracts.Protos.Identity;
using TICinema.Identity.Application.DTOs.Inputs;
using TICinema.Identity.Application.Interfaces;
using TICinema.Identity.Application.Interfaces.Repositories;
using TICinema.Identity.Application.Interfaces.Services;
using TICinema.Identity.Infrastructure.Configurations;

namespace TICinema.Identity.Grpc.Services;

public class AuthService(ILogger<AuthService> logger, IAuthService authService, ITelegramService telegramService, ITelegramRepository  telegramRepository, ICacheService cacheService, IOptions<TelegramSettings> tgOptions)
    : Contracts.Protos.Identity.AuthService.AuthServiceBase
{
    private readonly TelegramSettings _tgSettings = tgOptions.Value;
    
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

    public override async Task<TelegramInitResponse> TelegramInit(Empty request, ServerCallContext context)
    {
        var url = telegramService.GetAuthUrl();

        return new TelegramInitResponse
        {
            Url = url,
        };
    }

    public override async Task<TelegramVerifyResponse> TelegramVerify(TelegramVerifyRequest request, ServerCallContext context)
    {
        try
        {
            // Вся магия теперь здесь
            return await telegramService.VerifyAsync(request.Query);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Telegram verification failed: {Message}", ex.Message);
            throw new RpcException(new Status(StatusCode.Unauthenticated, ex.Message));
        }
    }

    public override async Task<TelegramCompleteResponse> TelegramComplete(TelegramCompleteRequest request, ServerCallContext context)
    {
        try
        {
            return await telegramService.CompleteAsync(request);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Telegram SessionId Getting Failed.");
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<TelegramConsumeResponse> TelegramConsume(TelegramConsumeRequest request, ServerCallContext context)
    {
        try
        {
            return await telegramService.ConsumeAsync(request);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex.Message);
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }
}