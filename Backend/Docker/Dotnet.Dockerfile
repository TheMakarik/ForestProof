# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Backend/Dotnet/ForestProof.Backend/ForestProof.Backend.csproj Backend/Dotnet/ForestProof.Backend/
RUN dotnet restore Backend/Dotnet/ForestProof.Backend/ForestProof.Backend.csproj

COPY Backend/Dotnet/ForestProof.Backend/ Backend/Dotnet/ForestProof.Backend/
RUN dotnet publish Backend/Dotnet/ForestProof.Backend/ForestProof.Backend.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY Backend/Dotnet/ForestProof.Backend/Dataset /data

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DataOptions__DataRoot=/data

EXPOSE 8080
ENTRYPOINT ["dotnet", "ForestProof.Backend.dll"]
