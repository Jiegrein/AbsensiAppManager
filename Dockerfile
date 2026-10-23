FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
COPY . /src
WORKDIR /src/AbsensiAppWebApi
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
COPY --from=build /app/publish /app
WORKDIR /app
# Container hosts (Heroku-style) pass the port in $PORT.
CMD ASPNETCORE_URLS=http://*:${PORT:-8080} dotnet AbsensiAppWebApi.dll
