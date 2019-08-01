FROM mcr.microsoft.com/dotnet/core/runtime:2.1-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR /src
COPY ["src/MySQLQueryDivider.Tests/MySQLQueryDivider.Tests.csproj", "src/MySQLQueryDivider.Tests/"]
COPY ["src/MySQLQueryDivider/MySQLQueryDivider.csproj", "src/MySQLQueryDivider/"]
RUN dotnet restore "src/MySQLQueryDivider.Tests/MySQLQueryDivider.Tests.csproj"
COPY . .
WORKDIR "/src/src/MySQLQueryDivider.Tests"
RUN dotnet test "MySQLQueryDivider.Tests.csproj" -c Debug

FROM build AS publish
RUN dotnet publish "MySQLQueryDivider.Tests.csproj" -c Debug -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MySQLQueryDivider.Tests.dll"]