FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM servicestack AS servicestack

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY SoundWords/SoundWords.csproj SoundWords/
RUN dotnet restore SoundWords/SoundWords.csproj
COPY . .
COPY --from=servicestack /ServiceStack /lib/ServiceStack
WORKDIR /src/SoundWords
RUN dotnet build SoundWords.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish SoundWords.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SoundWords.dll"]
