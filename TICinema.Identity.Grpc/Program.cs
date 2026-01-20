using Scalar.AspNetCore;
using TICinema.Identity.Application.DependencyInjection;
using TICinema.Identity.Grpc.Interceptors;
using TICinema.Identity.Grpc.Services;
using TICinema.Identity.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc(options =>
{
    // Подключаем наш перехватчик ко всем gRPC сервисам
    options.Interceptors.Add<ExceptionInterceptor>();
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddApplicationServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.MapGrpcService<AuthService>();
app.MapGrpcService<AccountService>();

app.MapControllers();

app.Run();