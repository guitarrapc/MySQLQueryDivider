FROM mcr.microsoft.com/dotnet/core/runtime:2.1-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR /src
COPY ["src/MySQLQueryDivider/MySQLQueryDivider.csproj", "src/MySQLQueryDivider/"]
RUN dotnet restore "src/MySQLQueryDivider/MySQLQueryDivider.csproj"
COPY . .
WORKDIR "/src/src/MySQLQueryDivider"
RUN dotnet build "MySQLQueryDivider.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "MySQLQueryDivider.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MySQLQueryDivider.dll"]