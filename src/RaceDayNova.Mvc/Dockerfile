# Multi-stage build keeps the runtime image small and excludes the SDK.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/RaceDayNova.Mvc/RaceDayNova.Mvc.csproj src/RaceDayNova.Mvc/
RUN dotnet restore src/RaceDayNova.Mvc/RaceDayNova.Mvc.csproj
COPY src/RaceDayNova.Mvc/ src/RaceDayNova.Mvc/
RUN dotnet publish src/RaceDayNova.Mvc/RaceDayNova.Mvc.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDayNova.Mvc.dll"]
