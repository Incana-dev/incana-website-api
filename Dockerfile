# Stage 1: Build the application
# Uses the .NET SDK to build your project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies first to leverage Docker's cache
COPY ["IncanaPortfolio.Api/IncanaPortfolio.Api.csproj", "IncanaPortfolio.Api/"]
COPY ["IncanaPortfolio.Data/IncanaPortfolio.Data.csproj", "IncanaPortfolio.Data/"]
RUN dotnet restore "IncanaPortfolio.Api/IncanaPortfolio.Api.csproj"

# Copy the rest of the source code and build the release
COPY . .
WORKDIR "/src/IncanaPortfolio.Api"
RUN dotnet build "IncanaPortfolio.Api.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "IncanaPortfolio.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Create the final, lightweight image
# Uses the smaller ASP.NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IncanaPortfolio.Api.dll"]