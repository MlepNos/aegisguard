# --- build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj files first
COPY ./backend/AegisGuard.Api/*.csproj            ./backend/AegisGuard.Api/
COPY ./backend/AegisGuard.Infrastructure/*.csproj ./backend/AegisGuard.Infrastructure/
COPY ./backend/AegisGuard.Domain/*.csproj         ./backend/AegisGuard.Domain/

RUN dotnet restore ./backend/AegisGuard.Api/AegisGuard.Api.csproj

# copy full source
COPY ./backend/AegisGuard.Api            ./backend/AegisGuard.Api
COPY ./backend/AegisGuard.Infrastructure ./backend/AegisGuard.Infrastructure
COPY ./backend/AegisGuard.Domain         ./backend/AegisGuard.Domain

RUN dotnet publish ./backend/AegisGuard.Api/AegisGuard.Api.csproj \
    -c Release -o /app/publish -p:UseAppHost=false

# --- runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "AegisGuard.Api.dll"]
