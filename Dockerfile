# Deterministic image for the web app. An explicit Dockerfile is preferred over
# the platform's auto-detection so the build does not try to publish the test
# project and the runtime version never drifts.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, with only the project file copied, so the layer is reused
# whenever the sources change but the dependencies do not.
COPY Mical/Mical.csproj Mical/
RUN dotnet restore Mical/Mical.csproj

COPY Mical/ Mical/
RUN dotnet publish Mical/Mical.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# The platform supplies PORT at runtime and the app binds to it (see
# HostingConfig.BindUrlFromPortVariable). 8080 is only the fallback for a plain
# `docker run` with no PORT set.
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "Mical.dll"]
