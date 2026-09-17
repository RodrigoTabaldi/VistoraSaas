FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore backend/Vistora.Worker/Vistora.Worker.csproj
RUN dotnet publish backend/Vistora.Worker/Vistora.Worker.csproj --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Vistora.Worker.dll"]
