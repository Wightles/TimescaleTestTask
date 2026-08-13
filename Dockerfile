FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TimescaleApi/TimescaleApi.csproj TimescaleApi/
RUN dotnet restore TimescaleApi/TimescaleApi.csproj

COPY TimescaleApi/ TimescaleApi/
WORKDIR /src/TimescaleApi
RUN dotnet publish TimescaleApi.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TimescaleApi.dll"]
