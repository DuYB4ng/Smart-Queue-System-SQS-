# 1. Base image cho runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# 2. Image cho build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy file csproj và restore (cache layer)
COPY ["SQS.API.csproj", "./"]
RUN dotnet restore "SQS.API.csproj"

# Copy toàn bộ code và build
COPY . .
WORKDIR "/src/"
RUN dotnet build "SQS.API.csproj" -c Release -o /app/build

# 3. Publish
FROM build AS publish
RUN dotnet publish "SQS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SQS.API.dll"]
