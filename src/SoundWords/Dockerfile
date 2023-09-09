FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM node AS node
WORKDIR /src
COPY SoundWords/bower.json .
COPY SoundWords/package.json .
COPY SoundWords/package-lock.json .
RUN npm install -g bower
RUN echo '{ "allow_root": true }' > /root/.bowerrc
RUN bower install
RUN npm install

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY SoundWords/SoundWords.csproj SoundWords/
RUN dotnet restore SoundWords/SoundWords.csproj
COPY --from=node /src/bower_components /src/SoundWords/wwwroot/lib
COPY --from=node /src/node_modules /src/SoundWords/node_modules
COPY . .
WORKDIR /src/SoundWords
RUN dotnet build SoundWords.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish SoundWords.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SoundWords.dll"]
