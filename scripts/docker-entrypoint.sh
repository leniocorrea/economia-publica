#!/bin/bash
set -e

echo "🚀 Iniciando aplicação EconomIA..."

# Executar migrations do banco de dados
if [ "$ASPNETCORE_ENVIRONMENT" = "Production" ]; then
  echo "📊 Executando migrations do banco de dados..."
    # Descomente a linha abaixo se sua aplicação tiver suporte a migrations via dotnet
      # dotnet ef database update || true
      fi

      # Iniciar aplicação
      echo "▶️ Iniciando aplicação .NET..."
      exec dotnet EconomIA.dll
