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

FROM node AS node
WORKDIR /src
COPY SoundWords/bower.json .
RUN npm install -g bower
RUN bower install

FROM build AS publish
COPY --from=node /src/bower_components /src/SoundWords/wwwroot/lib
RUN dotnet publish SoundWords.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SoundWords.dll"]
