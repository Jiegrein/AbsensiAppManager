FROM mcr.microsoft.com/dotnet/sdk:5.0 as build
COPY . /src
WORKDIR /src/AbsensiAppWebApi
RUN dotnet restore
RUN dotnet publish -r linux-x64 -c Release

FROM mcr.microsoft.com/dotnet/runtime:5.0 as runtime
COPY --from=build /src/AbsensiAppWebApi/bin/Release/net5.0/linux-x64/publish /app
WORKDIR /app
# ENTRYPOINT ["./AbsensiAppWebApi"]

# Use the following instead for Heroku
CMD ASPNETCORE_URLS=http://*:$PORT dotnet AbsensiAppWebApi.dll
