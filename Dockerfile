# ЭТАП 1: Сборка
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Копируем ВСЕ .csproj файлы, сохраняя структуру папок. 
# Это нужно, чтобы dotnet restore увидел зависимости между проектами.
COPY ["TICinema.Identity/TICinema.Identity.Grpc/TICinema.Identity.Grpc.csproj", "TICinema.Identity/TICinema.Identity.Grpc/"]
COPY ["TICinema.Identity/TICinema.Identity.Application/TICinema.Identity.Application.csproj", "TICinema.Identity/TICinema.Identity.Application/"]
COPY ["TICinema.Identity/TICinema.Identity.Domain/TICinema.Identity.Domain.csproj", "TICinema.Identity/TICinema.Identity.Domain/"]
COPY ["TICinema.Identity/TICinema.Identity.Infrastructure/TICinema.Identity.Infrastructure.csproj", "TICinema.Identity/TICinema.Identity.Infrastructure/"]
# Вот тут была главная ошибка в пути:
COPY ["TICinema.Contracts/TICinema.Contracts/TICinema.Contracts.csproj", "TICinema.Contracts/TICinema.Contracts/"]

# 2. Восстанавливаем зависимости
RUN dotnet restore "TICinema.Identity/TICinema.Identity.Grpc/TICinema.Identity.Grpc.csproj"

# 3. Копируем весь остальной код
COPY . .

# 4. Собираем проект
WORKDIR "/src/TICinema.Identity/TICinema.Identity.Grpc"
RUN dotnet build "TICinema.Identity.Grpc.csproj" -c Release -o /app/build

# ЭТАП 2: Публикация
FROM build AS publish
RUN dotnet publish "TICinema.Identity.Grpc.csproj" -c Release -o /app/publish

# ЭТАП 3: Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5000
EXPOSE 5001

ENTRYPOINT ["dotnet", "TICinema.Identity.Grpc.dll"]