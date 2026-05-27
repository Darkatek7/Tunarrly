FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Tunarrly.slnx ./
COPY Tunarrly.Core/Tunarrly.Core.csproj Tunarrly.Core/
COPY Tunarrly.Infrastructure/Tunarrly.Infrastructure.csproj Tunarrly.Infrastructure/
COPY Tunarrly.Web/Tunarrly.Web.csproj Tunarrly.Web/
RUN dotnet restore Tunarrly.slnx

COPY . .
RUN dotnet publish Tunarrly.Web/Tunarrly.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DATABASE__PATH=/app/data/tunarrly.db \
    LIBRARY__PATH=/music
RUN mkdir -p /app/data && chown -R $APP_UID:0 /app
COPY --from=build --chown=$APP_UID:0 /app/publish .
USER $APP_UID
EXPOSE 8080
VOLUME ["/app/data"]
ENTRYPOINT ["dotnet", "Tunarrly.Web.dll"]
