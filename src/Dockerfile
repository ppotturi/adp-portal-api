FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get upgrade -y
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN apt-get update && apt-get upgrade -y
WORKDIR /src
COPY ["./ADP.Portal.Api/ADP.Portal.Api.csproj", "./ADP.Portal.Api/"]
COPY ["./ADP.Portal.Core/ADP.Portal.Core.csproj", "./ADP.Portal.Core/"]
RUN dotnet restore "./ADP.Portal.Api/ADP.Portal.Api.csproj"
COPY . .
WORKDIR "/src/ADP.Portal.Api"
ARG BUILD_CONFIGURATION=Release
RUN dotnet build "./ADP.Portal.Api.csproj" -c "$BUILD_CONFIGURATION" -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ADP.Portal.Api.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ADP.Portal.Api.dll"]