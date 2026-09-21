FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/ECommerce.Domain/ECommerce.Domain.csproj", "src/ECommerce.Domain/"]
COPY ["src/ECommerce.Application/ECommerce.Application.csproj", "src/ECommerce.Application/"]
COPY ["src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj", "src/ECommerce.Infrastructure/"]
COPY ["src/ECommerce.Api/ECommerce.Api.csproj", "src/ECommerce.Api/"]

RUN dotnet restore "src/ECommerce.Api/ECommerce.Api.csproj"

COPY ["src/ECommerce.Domain/", "src/ECommerce.Domain/"]
COPY ["src/ECommerce.Application/", "src/ECommerce.Application/"]
COPY ["src/ECommerce.Infrastructure/", "src/ECommerce.Infrastructure/"]
COPY ["src/ECommerce.Api/", "src/ECommerce.Api/"]

WORKDIR /src/src/ECommerce.Api
RUN dotnet build "ECommerce.Api.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR /src/src/ECommerce.Api
RUN dotnet publish "ECommerce.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ECommerce.Api.dll"]
