# Byg API'en
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . . 
# COPY . /src - kunne man også have skrevet
RUN dotnet publish ./FleksProfitAPI.csproj -c Release -o /app

# Kør API'en
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FleksProfitAPI.dll"]