FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY EconomIA.Common/*.csproj EconomIA.Common/
COPY EconomIA.Common.EntityFramework/*.csproj EconomIA.Common.EntityFramework/
COPY EconomIA.Common.Fody/*.csproj EconomIA.Common.Fody/
COPY EconomIA.Domain/*.csproj EconomIA.Domain/
COPY EconomIA.Application/*.csproj EconomIA.Application/
COPY EconomIA.Adapters/*.csproj EconomIA.Adapters/
COPY EconomIA/*.csproj EconomIA/

RUN dotnet restore EconomIA/EconomIA.csproj

COPY . .

RUN dotnet build EconomIA.Common.Fody/EconomIA.Common.Fody.csproj -c Release
RUN dotnet publish EconomIA/EconomIA.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "EconomIA.dll"]
