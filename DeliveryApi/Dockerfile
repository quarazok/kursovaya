# === Этап 1: сборка проекта ===
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только csproj — чтобы кеш restore не ломался при изменении кода
COPY *.csproj ./
RUN dotnet restore

# Теперь весь исходный код
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# === Этап 2: финальный образ для запуска ===
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "DeliveryApi.dll"]
