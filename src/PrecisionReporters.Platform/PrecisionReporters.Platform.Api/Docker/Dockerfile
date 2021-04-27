FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /wrk
COPY . .
WORKDIR /wrk/src/PrecisionReporters.Platform/PrecisionReporters.Platform.Api
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS runtime
RUN apt-get update && apt-get -y install build-essential libssl1.1 libasound2 wget
RUN curl -LO https://github.com/DataDog/dd-trace-dotnet/releases/download/v1.26.0/datadog-dotnet-apm_1.26.0_amd64.deb &&\
    dpkg -i ./datadog-dotnet-apm_1.26.0_amd64.deb
RUN /opt/datadog/createLogPath.sh
WORKDIR /api
COPY --from=build /app .
EXPOSE 80
ENTRYPOINT ["dotnet", "PrecisionReporters.Platform.Api.dll"]
