# CLAUDE.md - Regras do Projeto EconomIA

## Regras de Deploy - CRÍTICO

### NUNCA fazer deploy direto em produção

**PROIBIDO:**
- Fazer `docker build` e `docker push` diretamente para imagens de produção
- Executar comandos `docker run/stop/rm` diretamente no servidor de produção
- Modificar banco de dados de produção diretamente

**OBRIGATÓRIO:**
- Todo código deve ser commitado e pushado para o repositório Git
- Deploy em produção SOMENTE via pipeline automático do Git (CI/CD)
- Testar SEMPRE em ambiente de desenvolvimento local antes de commitar
- Criar branch para features novas, fazer PR e merge para main

### Ambientes

- **DEV**: Máquina local com Docker Compose
- **PROD**: Servidor 136.113.233.79 - APENAS via deploy automático

### Fluxo de trabalho correto

1. Desenvolver e testar localmente
2. Commitar mudanças
3. Push para repositório remoto
4. Pipeline de CI/CD faz o deploy automático
5. Verificar logs e funcionamento em produção

## Build e Testes - IMPORTANTE

**NUNCA executar build ou testes diretamente no host.** O projeto usa .NET 10 que não está instalado localmente.

**SEMPRE usar Docker para build e testes:**
```bash
# Build
docker run --rm -v $(pwd):/app -w /app mcr.microsoft.com/dotnet/sdk:10.0-preview dotnet build

# Testes
docker run --rm -v $(pwd):/app -w /app mcr.microsoft.com/dotnet/sdk:10.0-preview dotnet test

# Build de projeto específico
docker run --rm -v $(pwd):/app -w /app mcr.microsoft.com/dotnet/sdk:10.0-preview dotnet build EconomIA.CargaDeDados/EconomIA.CargaDeDados.csproj
```

## Estrutura do Projeto

- `EconomIA/` - API REST principal
- `EconomIA.CargaDeDados/` - Worker de carga de dados do PNCP
- `EconomIA.Domain/` - Entidades de domínio
- `EconomIA.Application/` - Casos de uso e queries
- `EconomIA.Adapters/` - Repositórios e adaptadores

## Banco de Dados

- PostgreSQL para dados relacionais (snake_case para tabelas e colunas)
- Elasticsearch para busca de itens de compra
- Migrations via Entity Framework Core

## Convenções de Código

### Idioma
- Nomeie tudo em português (classes, variáveis, métodos), exceto termos técnicos consagrados
- Use artigos nos nomes: `InformacaoDaImagem` ao invés de `InformacaoImagem`

### Estilo C#
- Use `var` ao declarar variáveis
- Use nomes da CLR: `String`, `Int32`, `Boolean` (não `string`, `int`, `bool`)
- Use `is not null` ao invés de `!= null`
- Chaves `{` no fim da linha (estilo Java)
- Sempre use chaves em estruturas de controle, mesmo em blocos de uma linha
- Adicione linha em branco antes de estruturas de controle (if, while, for)
- Não use prefixo `_` em campos privados
- Evite comentários - código deve ser auto-explanatório

### SQL PostgreSQL
- Use letras minúsculas com `_` para separar: `itens_do_pedido`, `valor_unitario`

## Princípios
- SOLID quando fizer sentido
- Priorize legibilidade e manutenibilidade
- Prefira simplicidade sobre complexidade
- Evite over-engineering e abstrações prematuras

## Commits
- Mensagens de commit devem ser UMA ÚNICA FRASE curta em português
- Formato: `<tipo>: <descrição em uma frase>`
- Tipos: feat, fix, refactor, style, test, docs, chore
- NUNCA usar múltiplas linhas ou bullet points no commit
- Não adicionar co-autoria ou indicação de geração por IA
- Sempre aguardar aprovação do usuário antes de executar o commit
