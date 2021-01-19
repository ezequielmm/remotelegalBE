FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /wrk
COPY . .
WORKDIR /wrk/src/PrecisionReporters.Platform/PrecisionReporters.Platform.Api
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS runtime

# Install the new relic agent
RUN apt-get update && apt-get install -y wget ca-certificates gnupg \
&& echo 'deb http://apt.newrelic.com/debian/ newrelic non-free' | tee /etc/apt/sources.list.d/newrelic.list \
&& wget https://download.newrelic.com/548C16BF.gpg \
&& apt-key add 548C16BF.gpg \
&& apt-get update \
&& apt-get install -y newrelic-netcore20-agent

# Enable the new relic agent
ENV CORECLR_ENABLE_PROFILING=1 \
CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
CORECLR_NEWRELIC_HOME=/usr/local/newrelic-netcore20-agent \
CORECLR_PROFILER_PATH=/usr/local/newrelic-netcore20-agent/libNewRelicProfiler.so \
NEW_RELIC_LICENSE_KEY=NEW_RELIC_LICENSE_KEY_VALUE \
NEW_RELIC_APP_NAME=PrecisionReporters

WORKDIR /api
COPY --from=build /app .
EXPOSE 80
ENTRYPOINT ["dotnet", "PrecisionReporters.Platform.Api.dll"]
