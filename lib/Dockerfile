FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src

COPY src/ServiceStack.OrmLite/src/ServiceStack.OrmLite.Sqlite/ServiceStack.OrmLite.Sqlite.csproj ServiceStack.OrmLite/src/ServiceStack.OrmLite.Sqlite/
RUN dotnet restore ServiceStack.OrmLite/src/ServiceStack.OrmLite.Sqlite/ServiceStack.OrmLite.Sqlite.csproj

COPY src/ServiceStack.OrmLite/src/ServiceStack.OrmLite.SqlServer/ServiceStack.OrmLite.SqlServer.csproj ServiceStack.OrmLite/src/ServiceStack.OrmLite.SqlServer/
RUN dotnet restore ServiceStack.OrmLite/src/ServiceStack.OrmLite.SqlServer/ServiceStack.OrmLite.SqlServer.csproj

COPY src/ServiceStack.OrmLite/src/ServiceStack.OrmLite.PostgreSQL/ServiceStack.OrmLite.PostgreSQL.csproj ServiceStack.OrmLite/src/ServiceStack.OrmLite.PostgreSQL/
RUN dotnet restore ServiceStack.OrmLite/src/ServiceStack.OrmLite.PostgreSQL/ServiceStack.OrmLite.PostgreSQL.csproj

COPY src/ServiceStack.OrmLite/src/ServiceStack.OrmLite.MySql/ServiceStack.OrmLite.MySql.csproj ServiceStack.OrmLite/src/ServiceStack.OrmLite.MySql/
RUN dotnet restore ServiceStack.OrmLite/src/ServiceStack.OrmLite.MySql/ServiceStack.OrmLite.MySql.csproj

COPY src/ServiceStack.Redis/src/ServiceStack.Redis/ServiceStack.Redis.csproj ServiceStack.Redis/src/ServiceStack.Redis/
RUN dotnet restore ServiceStack.Redis/src/ServiceStack.Redis/ServiceStack.Redis.csproj

COPY src/ServiceStack/src/ServiceStack/ServiceStack.csproj ServiceStack/src/ServiceStack/
RUN dotnet restore ServiceStack/src/ServiceStack/ServiceStack.csproj

COPY src/ServiceStack/src/ServiceStack.Server/ServiceStack.Server.csproj ServiceStack/src/ServiceStack.Server/
RUN dotnet restore ServiceStack/src/ServiceStack.Server/ServiceStack.Server.csproj

COPY src/ServiceStack/src/ServiceStack.Mvc/ServiceStack.Mvc.csproj ServiceStack/src/ServiceStack.Mvc/
RUN dotnet restore ServiceStack/src/ServiceStack.Mvc/ServiceStack.Mvc.csproj

COPY src/ServiceStack/src/ServiceStack.Common/ServiceStack.Common.csproj ServiceStack/src/ServiceStack.Common/
RUN dotnet restore ServiceStack/src/ServiceStack.Common/ServiceStack.Common.csproj

COPY src src

RUN dotnet build src/ServiceStack.OrmLite/src/ServiceStack.OrmLite.Sqlite/ServiceStack.OrmLite.Sqlite.csproj -c Release -f netstandard2.0 -o /ServiceStack

RUN dotnet build src/ServiceStack.OrmLite/src/ServiceStack.OrmLite.SqlServer/ServiceStack.OrmLite.SqlServer.csproj -c Release -f netstandard2.0 -o /ServiceStack

RUN dotnet build src/ServiceStack.OrmLite/src/ServiceStack.OrmLite.PostgreSQL/ServiceStack.OrmLite.PostgreSQL.csproj -c Release -f netstandard2.0 -o /ServiceStack

RUN dotnet build src/ServiceStack.OrmLite/src/ServiceStack.OrmLite.MySql/ServiceStack.OrmLite.MySql.csproj -c Release -f netstandard2.0 -o /ServiceStack

RUN dotnet build src/ServiceStack.Redis/src/ServiceStack.Redis/ServiceStack.Redis.csproj -c Release -f netstandard2.0 -o /ServiceStack

RUN dotnet build src/ServiceStack/src/ServiceStack/ServiceStack.csproj -c Release -f netstandard2.0 -o /ServiceStack

RUN dotnet build src/ServiceStack/src/ServiceStack.Server/ServiceStack.Server.csproj -c Release -f netstandard2.0 -o /ServiceStack

RUN dotnet build src/ServiceStack/src/ServiceStack.Mvc/ServiceStack.Mvc.csproj -c Release -f netstandard2.0 -o /ServiceStack

RUN dotnet build src/ServiceStack/src/ServiceStack.Common/ServiceStack.Common.csproj -c Release -f netstandard2.0 -o /ServiceStack

FROM alpine
COPY --from=build /ServiceStack ServiceStack

CMD [ "/bin/true" ]
