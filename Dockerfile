FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 2685

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . ./
COPY ["UserManager.csproj", "."]
RUN dotnet restore "./UserManager.csproj"
WORKDIR "/src/."
RUN dotnet build "./UserManager.csproj" 

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./UserManager.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserManager.dll"]
