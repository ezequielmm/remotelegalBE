FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /wrk
COPY . .
WORKDIR /wrk/src/PrecisionReporters.Platform/PrecisionReporters.Platform.Api
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine
WORKDIR /api
COPY --from=build /app .
EXPOSE 80
ENTRYPOINT ["dotnet", "PrecisionReporters.Platform.Api.dll"]
