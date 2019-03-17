FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM servicestack AS servicestack

FROM node AS node
WORKDIR /src
COPY SoundWords/bower.json .
COPY SoundWords/package.json .
COPY SoundWords/package-lock.json .
RUN npm install -g bower
RUN echo '{ "allow_root": true }' > /root/.bowerrc
RUN bower install
RUN npm install

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY SoundWords/SoundWords.csproj SoundWords/
RUN dotnet restore SoundWords/SoundWords.csproj
COPY --from=node /src/bower_components /src/SoundWords/wwwroot/lib
COPY --from=node /src/node_modules /src/SoundWords/node_modules
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
