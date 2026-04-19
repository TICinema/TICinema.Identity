using MassTransit;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Scalar.AspNetCore;
using TICinema.Contracts.Protos.Users;
using TICinema.Identity.Application.DependencyInjection;
using TICinema.Identity.Grpc.Interceptors;
using TICinema.Identity.Grpc.Services;
using TICinema.Identity.Infrastructure.DependencyInjection;
using TICinema.Notification.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5101, o =>
    {
        o.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Identity-Service"))
        .AddAspNetCoreInstrumentation()
        .AddGrpcClientInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRedisInstrumentation()
        .AddOtlpExporter(options => options.Endpoint = new Uri("http://jaeger:4317")));

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ExceptionInterceptor>();
});

builder.Services.AddOpenApi();

builder.Services.AddInfrastructureServices(builder.Configuration);

var rmqSettings = builder.Configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(rmqSettings!.Url), h =>
        {
        });
    });
});

builder.Services.AddGrpcClient<UsersService.UsersServiceClient>(o =>
{
    o.Address = new Uri(builder.Configuration["GrpcSettings:UsersServiceUrl"]!);
});

builder.Services.AddGrpcReflection();

builder.Services.AddApplicationServices();

var app = builder.Build();

app.UseRouting();

app.UseHttpMetrics();

app.UseGrpcMetrics();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.MapGrpcService<AuthService>();
app.MapGrpcService<AccountService>();

app.MapMetrics();

app.Run();