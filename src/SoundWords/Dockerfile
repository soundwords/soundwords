FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY SoundWords/SoundWords.csproj SoundWords/
COPY SoundWords/libman.json SoundWords/
RUN dotnet restore SoundWords/SoundWords.csproj
COPY . .
WORKDIR /src/SoundWords
RUN dotnet publish SoundWords.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SoundWords.dll"]
