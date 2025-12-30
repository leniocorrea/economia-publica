# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar solution e projetos
COPY EconomIA.slnx ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY EconomIA.CargaDeDados/EconomIA.CargaDeDados.csproj EconomIA.CargaDeDados/

# Restaurar dependencias
RUN dotnet restore "EconomIA.CargaDeDados/EconomIA.CargaDeDados.csproj"

# Copiar codigo fonte
COPY . .

# Build
WORKDIR /src/EconomIA.CargaDeDados
RUN dotnet build "EconomIA.CargaDeDados.csproj" -c Release -o /app/build

# Publish
RUN dotnet publish "EconomIA.CargaDeDados.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EconomIA.CargaDeDados.dll"]
