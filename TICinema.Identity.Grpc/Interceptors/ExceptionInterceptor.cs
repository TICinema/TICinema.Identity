using Grpc.Core;
using Grpc.Core.Interceptors;

namespace TICinema.Identity.Grpc.Interceptors;

public class ExceptionInterceptor(ILogger<ExceptionInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            // Выполняем сам метод сервиса
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            // Если мы уже выбросили RpcException вручную — просто пропускаем её дальше
            throw;
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Ошибка валидации аргументов");
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            // Все остальные непредвиденные ошибки (база, сеть и т.д.)
            logger.LogError(ex, "Необработанное исключение в gRPC сервисе");
            
            // Не отдаем детали системной ошибки клиенту в целях безопасности
            throw new RpcException(new Status(StatusCode.Internal, "Внутренняя ошибка сервера"));
        }
    }
}