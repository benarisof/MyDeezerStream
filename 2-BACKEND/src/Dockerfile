# ÉTAPE 1 : Build avec SDK 9.0
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copier les fichiers projet
COPY ["MyDeezerStream.API/MyDeezerStream.API.csproj", "MyDeezerStream.API/"]
COPY ["MyDeezerStream.Application/MyDeezerStream.Application.csproj", "MyDeezerStream.Application/"]
COPY ["MyDeezerStream.Domain/MyDeezerStream.Domain.csproj", "MyDeezerStream.Domain/"]
COPY ["MyDeezerStream.Infrastructure/MyDeezerStream.Infrastructure.csproj", "MyDeezerStream.Infrastructure/"]

# Restaurer les dépendances
RUN dotnet restore "MyDeezerStream.API/MyDeezerStream.API.csproj"

# Copier tout le reste du code
COPY . .

# Publier l'application
WORKDIR "/src/MyDeezerStream.API"
RUN dotnet publish "MyDeezerStream.API.csproj" -c Release -o /app/publish

# ÉTAPE 2 : Runtime avec ASP.NET 9.0
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copier les fichiers publiés
COPY --from=build /app/publish .

# Exposer le port
EXPOSE 5257

# Lancer l'application
ENTRYPOINT ["dotnet", "MyDeezerStream.API.dll"]