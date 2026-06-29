# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Kopier projektfiler og gendan afhaengigheder foerst (caches hvis de ikke aendrer sig).
COPY WasteEventLib/WasteEventLib.csproj WasteEventLib/
COPY WasteEventApi/WasteEventApi.csproj WasteEventApi/
RUN dotnet restore WasteEventApi/WasteEventApi.csproj

# Kopier resten af koden og publicer en release-build.
COPY WasteEventLib/ WasteEventLib/
COPY WasteEventApi/ WasteEventApi/
RUN dotnet publish WasteEventApi/WasteEventApi.csproj -c Release -o /app

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Lyt paa port 8080 inde i containeren.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WasteEventApi.dll"]
