# API — Listagem de Atas

`GET /v1/atas`

Lista atas de registro de preço por período, direto do PostgreSQL. Complementa `GET /v1/itens-da-compra`: a busca de itens parte do índice Elasticsearch e só enxerga atas cuja compra foi indexada — este endpoint parte da tabela `ata`, então **também devolve as atas cuja compra não está no índice**.

- **Método:** `GET`
- **Autenticação:** nenhuma (endpoint público)
- **Tag OpenAPI:** `Atas`

---

## Como funciona

1. O período é resolvido sobre a **data de referência** da ata: `data_assinatura` quando existe, senão `data_publicacao_pncp`.
2. Sem `dataInicio`/`dataFim`, o endpoint aplica automaticamente os **últimos 7 dias**. O período efetivamente usado volta no bloco `periodo` da resposta.
3. A ordenação é fixa: data de referência **decrescente**, com o identificador como desempate.
4. A paginação é por keyset (`cursor` opaco) — estável mesmo com carga concorrente e sem teto de profundidade.
5. Cada ata recebe um bloco `adesao` calculado no servidor, com a mesma forma usada em `/v1/itens-da-compra`.

---

## Parâmetros (query string)

| Parâmetro | Tipo | Padrão | Descrição |
|---|---|---|---|
| `dataInicio` | date | `dataFim` − 7 dias | Limite inferior da data de referência. Tratado como data (dia inteiro). |
| `dataFim` | date | hoje | Limite superior. O dia informado é **incluído por inteiro**. |
| `vigentesEm` | date | — | Mantém apenas atas com `vigenciaFim >= data`. Use a data de hoje para "ainda válidas". |
| `apenasNaoCanceladas` | bool | `true` | Exclui atas canceladas. Passe `false` para incluí-las. |
| `apenasComAdesao` | bool | — | Apenas atas com `possibilidadeAdesao = true`. |
| `cnpjOrgao` | string | — | Filtra pelo CNPJ do órgão. |
| `limit` | int | 50 | Tamanho da página (teto 1000). |
| `cursor` | string | — | Cursor opaco. Use o `nextCursor` da resposta anterior. |

Não há parâmetro `order`: a ordenação é sempre por data de referência decrescente.

**Fuso:** "hoje" é calculado em horário de Brasília (UTC−3), igual ao resto da API.

---

## Estrutura da resposta

```
{
  items: [
    {
      numeroControlePncpAta, numeroAtaRegistroPreco, anoAta, objetoContratacao,
      cancelada, dataAssinatura, dataPublicacaoPncp, dataDeReferencia,
      vigenciaInicio, vigenciaFim, possibilidadeAdesao,
      numeroControlePncpCompra,
      orgaoEntidade: { cnpj, razaoSocial, nomeFantasia, poderId, esferaId } | null,
      adesao: { situacao, disponivel, vigenciaFim, diasRestantes, numeroControlePncpAta, saldoDisponivel }
    }
  ],
  hasMoreItems: boolean,
  nextCursor: string | null,
  periodo: { inicio, fim }
}
```

### Campos que merecem atenção

| Campo | Observação |
|---|---|
| `dataDeReferencia` | A data que foi usada para filtrar e ordenar (`dataAssinatura` ou, na falta dela, `dataPublicacaoPncp`). Deixa explícito para o front por que a ata caiu naquela posição. |
| `numeroControlePncpCompra` | Pode ser `null` — é justamente a ata sem compra correspondente, invisível na busca por itens. |
| `periodo` | Janela efetivamente aplicada. Útil quando o cliente não informou datas e recebeu o padrão de 7 dias. |
| `orgaoEntidade` | **Não traz UF.** A ata não tem UF própria e a unidade do órgão é ambígua em órgãos multi-estaduais — preferimos omitir a dar um dado impreciso. |

### `adesao`

Mesma estrutura de `/v1/itens-da-compra`, avaliada sobre a própria ata:

| `situacao` | Quando |
|---|---|
| `permitida` | ata vigente e não cancelada, com `possibilidadeAdesao = true` |
| `nao_permitida` | ata vigente e não cancelada, com `possibilidadeAdesao = false` |
| `nao_informada` | ata vigente e não cancelada, sem a informação no PNCP |
| `sem_ata` | ata cancelada ou com vigência expirada |

> `sem_ata` soa estranho quando você está olhando para uma ata específica — é o vocabulário herdado da avaliação no nível do item. Só aparece se você passar `apenasNaoCanceladas=false` ou listar atas já vencidas.

---

## Erros

| Situação | Status | `code` |
|---|---|---|
| `dataInicio > dataFim` | 400 | `InvalidArgument` |
| `limit` fora de 1..1000 | 400 | `InvalidArgument` |
| `cursor` malformado | 400 | `InvalidAtaRequest` |

---

## Exemplos

Atas da última semana (padrão):

```
GET /v1/atas
```

Atas assinadas em julho que continuam vigentes hoje e aceitam carona:

```
GET /v1/atas?dataInicio=2026-07-01&dataFim=2026-07-31&vigentesEm=2026-08-06&apenasComAdesao=true
```

Próxima página:

```
GET /v1/atas?cursor=MTYzODM2ODAwMDAwMDAwMDAwfDQzMjE=
```

---

## Índices

O endpoint depende de dois índices criados pela migration `AdicionarIndicesDeDataNaAta`:

- `ix_ata_data_de_referencia` — expressão `coalesce(data_assinatura, data_publicacao_pncp) DESC, identificador DESC`, casando exatamente com o `ORDER BY` do keyset.
- `ix_ata_vigencia_fim` — parcial (`where cancelado = false`), para o filtro `vigentesEm`.
