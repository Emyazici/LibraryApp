# syntax=docker/dockerfile:1
# Build context: repository root
#   docker build -f LibraryApp.API/Dockerfile -t libraryapp-api .

# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only project files first so `dotnet restore` is cached
# until a csproj changes.
COPY LibraryApp.sln ./
COPY LibraryApp.Domain/LibraryApp.Domain.csproj LibraryApp.Domain/
COPY LibraryApp.Application/LibraryApp.Application.csproj LibraryApp.Application/
COPY LibraryApp.Infrastructure/LibraryApp.Infrastructure.csproj LibraryApp.Infrastructure/
COPY LibraryApp.API/LibraryApp.API.csproj LibraryApp.API/
RUN dotnet restore LibraryApp.API/LibraryApp.API.csproj

# Copy the source of the projects the API depends on and publish.
COPY LibraryApp.Domain/ LibraryApp.Domain/
COPY LibraryApp.Application/ LibraryApp.Application/
COPY LibraryApp.Infrastructure/ LibraryApp.Infrastructure/
COPY LibraryApp.API/ LibraryApp.API/
RUN dotnet publish LibraryApp.API/LibraryApp.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

COPY --from=build /app/publish .

# Non-root user shipped with the .NET 8 images (UID 1654)
USER app

ENTRYPOINT ["dotnet", "LibraryApp.API.dll"]
