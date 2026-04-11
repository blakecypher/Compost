# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY Compost.sln ./
COPY src/Compost.Web/Compost.Web.csproj src/Compost.Web/
COPY src/Compost.Core/Compost.Core.csproj src/Compost.Core/
COPY src/Modules/Compost.Contexts/Compost.Contexts.csproj src/Modules/Compost.Contexts/
COPY src/Modules/Compost.MindMap/Compost.MindMap.csproj src/Modules/Compost.MindMap/
COPY src/Modules/Compost.Transcription/Compost.Transcription.csproj src/Modules/Compost.Transcription/
COPY src/Modules/Compost.Kanban/Compost.Kanban.csproj src/Modules/Compost.Kanban/
COPY src/Modules/Compost.Snippets/Compost.Snippets.csproj src/Modules/Compost.Snippets/
COPY src/Modules/Compost.Patterns/Compost.Patterns.csproj src/Modules/Compost.Patterns/
COPY src/Modules/Compost.Analytics/Compost.Analytics.csproj src/Modules/Compost.Analytics/
COPY src/Modules/Compost.Structure/Compost.Structure.csproj src/Modules/Compost.Structure/
COPY src/Themes/Compost.Theme/Compost.Theme.csproj src/Themes/Compost.Theme/

# Restore dependencies
RUN dotnet restore src/Compost.Web/Compost.Web.csproj

# Copy the rest of the source code
COPY . .

# Build and publish
RUN dotnet publish src/Compost.Web/Compost.Web.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Copy published output
COPY --from=build /app/publish .

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Compost.Web.dll"]
