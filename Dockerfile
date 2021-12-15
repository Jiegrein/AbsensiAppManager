FROM mcr.microsoft.com/dotnet/sdk:5.0 as build
COPY . /src
WORKDIR /src/AbesensiAppWebApi
RUN dotnet restore
RUN dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true

FROM mcr.microsoft.com/dotnet/runtime:5.0 as runtime
COPY --from=build /src/AbesensiAppWebApi/bin/Release/net5.0/linux-x64/publish /app
WORKDIR /app

# ENTRYPOINT [ "dotnet", "HerokuApp.dll" ]
# Use the following instead for Heroku
CMD ASPNETCORE_URLS=http://*:$PORT dotnet AbsensiAppWebApi.dll
