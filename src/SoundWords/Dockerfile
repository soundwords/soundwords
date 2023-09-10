FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY SoundWords/SoundWords.csproj SoundWords/
COPY SoundWords/libman.json SoundWords/
RUN dotnet restore SoundWords/SoundWords.csproj
COPY . .
WORKDIR /src/SoundWords
RUN dotnet build SoundWords.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish SoundWords.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SoundWords.dll"]
