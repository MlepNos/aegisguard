# --- build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only the project file first (better layer caching)
COPY ./frontend/AegisGuard.Blazor/*.csproj ./frontend/AegisGuard.Blazor/
# copy any referenced project csproj files too, if applicable:
# COPY ./Shared/Shared.csproj ./Shared/

RUN dotnet restore ./frontend/AegisGuard.Blazor/AegisGuard.Blazor.csproj

# Now copy the rest of the sources
COPY ./frontend/AegisGuard.Blazor ./frontend/AegisGuard.Blazor
# COPY ./Shared ./Shared

RUN dotnet publish ./frontend/AegisGuard.Blazor/AegisGuard.Blazor.csproj -c Release -o /app/publish

# --- runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 5270
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://0.0.0.0:5270
ENTRYPOINT ["dotnet", "AegisGuard.Blazor.dll"]
