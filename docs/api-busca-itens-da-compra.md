# API — Busca de Itens da Compra

`GET /v1/itens-da-compra`

Busca itens de compras públicas (indexados no Elasticsearch a partir dos dados do PNCP) e devolve, agrupados por compra, os itens que casam com a descrição pesquisada — junto com o órgão, os resultados homologados (fornecedor/valor), as **atas de registro de preço** da compra e um bloco derivado de **disponibilidade para adesão (carona)**.

- **Método:** `GET` (também aceita `HEAD`)
- **Autenticação:** nenhuma (endpoint público)
- **Tag OpenAPI:** `Itens da Compra`

---

## Como funciona

1. O parâmetro `descricao` é enviado ao índice Elasticsearch `itens-da-compra` como uma busca textual (match com `fuzziness = AUTO`). **Quando omitido, não há filtro de texto** e o endpoint lista todas as compras que casam com os demais filtros. Os filtros `razaoSocial`, `ufSigla` e `dataInclusao*` também são resolvidos **dentro** do Elasticsearch.
2. O Elasticsearch devolve os identificadores dos itens da página atual (paginação por cursor/offset).
3. Esses itens são hidratados no PostgreSQL com sua compra, órgão, unidade e resultados homologados.
4. Para cada compra são carregadas as **atas** (ligação `ata.numero_controle_pncp_compra = compra.numero_controle_pncp`) e os contratos.
5. Para cada item, o servidor calcula o bloco `adesao` (ver adiante).
6. A resposta é **agrupada por compra**: itens da mesma compra vêm juntos, e as atas aparecem uma única vez no nível da compra.

---

## Parâmetros (query string)

| Parâmetro | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `descricao` | string | não | Texto buscado na descrição do item (match difuso). **Omitido ou vazio → lista sem filtro de texto**, ordenando por data de inclusão da compra (mais recentes primeiro). |
| `ufSigla` | string | não | Filtra pela UF da unidade do órgão (ex.: `SP`). Resolvido no Elasticsearch. |
| `razaoSocial` | string | não | Filtra pela razão social do órgão (match). Resolvido no Elasticsearch. |
| `dataInclusaoInicio` | date-time | não | Limite inferior da data de inclusão da compra. |
| `dataInclusaoFim` | date-time | não | Limite superior da data de inclusão da compra. |
| `valorUnitarioHomologadoMinimo` | decimal | não | Mantém itens com **algum** resultado de valor unitário homologado ≥ ao informado. |
| `valorUnitarioHomologadoMaximo` | decimal | não | Idem, valor unitário homologado ≤ ao informado. |
| `valorTotalHomologadoMinimo` | decimal | não | Idem, valor total homologado ≥ ao informado. |
| `valorTotalHomologadoMaximo` | decimal | não | Idem, valor total homologado ≤ ao informado. |
| `apenasComAdesao` | bool | não | Mantém apenas itens com ata vigente **e adesão permitida** (`adesao.situacao = "permitida"`). Resolvido no Elasticsearch. |
| `apenasComAtaVigente` | bool | não | Mantém apenas itens com **alguma ata vigente** (não cancelada, `vigenciaFim >= hoje`), permitindo adesão ou não. Resolvido no Elasticsearch. |
| `dataDaAtaInicio` | date | não | Limite inferior da **data de referência da ata** (assinatura; na falta, publicação no PNCP) — a mesma usada em `GET /v1/atas`. Tratado como data (dia inteiro). |
| `dataDaAtaFim` | date | não | Limite superior da data de referência da ata. O dia informado é **incluído por inteiro**. `dataDaAtaInicio > dataDaAtaFim` → `400`. |
| `cnpjOrgao` | string | não | **Modo compra específica.** CNPJ do órgão comprador, com ou sem máscara. Exige `anoCompra` e `sequencialCompra`. |
| `anoCompra` | int | não | **Modo compra específica.** Ano do edital. Exige `cnpjOrgao` e `sequencialCompra`. |
| `sequencialCompra` | int | não | **Modo compra específica.** Sequencial do processo no órgão naquele ano. Exige `cnpjOrgao` e `anoCompra`. |
| `limit` | int | não | Tamanho da página (padrão `20`). |
| `cursor` | string | não | Cursor de paginação (offset numérico). Use o `nextCursor` da resposta anterior. |
| `order` | string | não | Aceito, mas **não aplicado**: com `descricao`, ordena por relevância; sem `descricao`, por `dataInclusao` decrescente — ou pela **data da ata** decrescente quando qualquer filtro de ata (`apenasComAdesao`, `apenasComAtaVigente`, `dataDaAta*`) está ativo. |

> **Filtros de ata são do item, não da compra.** Todos os três olham a ata **mais recente, vigente e não cancelada** da compra, e só valem para itens com resultado homologado (mesma regra do bloco `adesao`). Eles são resolvidos no índice, então `totalHits` e a paginação refletem o filtro. Com `descricao`, a busca textual continua por relevância e os filtros de ata apenas restringem o conjunto.

> **Atenção — filtros de valor:** os quatro filtros `valor*Homologado*` são aplicados **em memória, depois** da paginação do Elasticsearch. Eles reduzem os itens retornados na página, mas **não alteram o `totalHits`** (que vem do Elasticsearch). Para uma contagem exata filtrada por valor, isso ainda não é suportado.

---

## Modo compra específica — todos os itens de um edital

No PNCP uma compra **não tem id sequencial global**. Ela é identificada por uma tripla: **CNPJ do órgão + ano da compra + sequencial da compra**. Informando os três parâmetros juntos, o endpoint devolve **todos os itens daquele edital**, e não apenas os que casaram com uma busca textual.

```
GET /v1/itens-da-compra?cnpjOrgao=88600655000141&anoCompra=2026&sequencialCompra=316&limit=1000
```

O front tem esses três dados em qualquer item já exibido na tela:

| Parâmetro | De onde vem na resposta |
|---|---|
| `cnpjOrgao` | `resultado[].orgaoEntidade.cnpj` |
| `anoCompra` | `resultado[].compra.anoCompra` |
| `sequencialCompra` | `resultado[].compra.sequencialCompra` |

**Como este modo se comporta:**

- **Não passa pelo Elasticsearch.** A consulta vai direto ao PostgreSQL pela chave da compra (índice único `identificador_do_orgao + ano_compra + sequencial_compra`).
- **Os três são tudo ou nada.** Informar um ou dois deles retorna `400`.
- **O CNPJ é normalizado:** `88.600.655/0001-41` e `88600655000141` encontram a mesma compra.
- **Ordenação por `numeroItem` crescente** — a ordem natural do edital, não por relevância.
- **`totalHits` é exato** (o total de itens do edital, já considerando os filtros aplicados) e a paginação por `cursor` funciona normalmente. O `limit` máximo continua sendo `1000`; editais maiores exigem seguir o `nextCursor`.
- Como todos os itens são da mesma compra, `resultado` traz **um único elemento** com todos os itens dentro de `compra.itemDaCompra`.

**Filtros combinados com a tripla:**

| Filtro | Comportamento neste modo |
|---|---|
| `descricao` | Aplicado como **busca por substring, sem acento-insensibilidade e sem correção de digitação**, sobre a descrição dos itens do edital. |
| `objetoDaCompra` | Aplicado como substring sobre o objeto da compra — como todos os itens são da mesma compra, é tudo ou nada. |
| `valor*Homologado*` | Aplicados normalmente, e aqui **entram no `totalHits`**. |
| `ufSigla`, `razaoSocial`, `dataInclusao*`, `dataDaAta*`, `apenasComAdesao`, `apenasComAtaVigente` | **Ignorados** — são resolvidos no Elasticsearch, que não participa deste modo. |

> ⚠️ **`descricao` muda de semântica neste modo.** Na busca normal ela é um match difuso no Elasticsearch (`fuzziness = AUTO`), que tolera erros de digitação. Aqui é uma comparação literal de substring: `descricao=notbook` acha "notebook" na busca normal e **não acha nada** no modo compra específica. Se a intenção do botão é "carregar todos os itens deste edital", **não envie `descricao`**.

---

## Estrutura da resposta

```
{
  resultado: [
    {
      orgaoEntidade: { cnpj, razaoSocial, poderId, esferaId, unidadeDoOrgao: { codigoIbge } } | null,
      compra: {
        anoCompra, sequencialCompra, numeroCompra, processo, objetoCompra,
        ufNome, dataInclusao, dataAberturaProposta, dataEncerramentoProposta,
        linkSistemaOrigem, numeroControlePNCP, modalidadeNome, situacaoCompraNome, ufSigla,
        atas: [ AtaDaCompra ],          // novo
        itemDaCompra: [ ItemDaCompra ]
      }
    }
  ],
  totalHits: number,        // total do Elasticsearch (ver observação sobre saturação)
  hasMoreItems: boolean,
  nextCursor: string | null
}
```

### `AtaDaCompra` (nível da compra)

Uma compra por registro de preço (SRP) pode ter **N atas** — por isso é uma lista no nível da compra.

| Campo | Tipo | Descrição |
|---|---|---|
| `numeroControlePncpAta` | string | Número de controle PNCP da ata (`CNPJ-1-SEQ/ANO-SEQATA`). |
| `numeroAtaRegistroPreco` | string \| null | Número da ARP no sistema de origem. |
| `anoAta` | int | Ano da ata. |
| `objetoContratacao` | string \| null | Objeto da contratação. |
| `cancelada` | bool | Se a ata está cancelada. |
| `vigenciaInicio` | date-time \| null | Início da vigência. |
| `vigenciaFim` | date-time \| null | Fim da vigência. |

> A lista `atas[]` traz **todas** as atas da compra (inclusive vencidas ou canceladas), como informação bruta. A avaliação de vigência/adesão fica no bloco `adesao` de cada item.

### `ItemDaCompra`

| Campo | Tipo | Descrição |
|---|---|---|
| `numeroItem` | int | Número do item na compra. |
| `descricao` | string \| null | Descrição do item. |
| `materialOuServico` | string \| null | (Não populado atualmente.) |
| `valorUnitarioEstimado` | decimal \| null | Valor unitário estimado. |
| `valorTotal` | decimal \| null | Valor total estimado. |
| `quantidade` | decimal \| null | Quantidade. |
| `unidadeMedida` | string \| null | Unidade de medida. |
| `criterioJulgamentoNome` | string \| null | Critério de julgamento. |
| `situacaoCompraItemNome` | string \| null | Situação do item. |
| `resultado` | `ResultadoItem[]` | Fornecedores vencedores e valores homologados. |
| `adesao` | `Adesao` | **Novo** — bloco derivado de disponibilidade para adesão. |

### `ResultadoItem`

| Campo | Tipo | Descrição |
|---|---|---|
| `numeroItem` | int | Número do item. |
| `niFornecedor` | string \| null | CNPJ/CPF do fornecedor. |
| `tipoPessoa` | string \| null | (Não populado atualmente.) |
| `nomeRazaoSocialFornecedor` | string \| null | Fornecedor vencedor. |
| `porteFornecedorId` | string \| null | (Não populado atualmente.) |
| `quantidadeHomologada` | decimal \| null | Quantidade homologada. |
| `valorUnitarioHomologado` | decimal \| null | Valor unitário homologado. |
| `valorTotalHomologado` | decimal \| null | Valor total homologado. |
| `ordemClassificacaoSrp` | int \| null | (Não populado atualmente.) |
| `numeroControlePNCPCompra` | string | Número de controle PNCP da compra. |

### `Adesao` (nível do item)

Bloco pré-calculado no servidor para o front estampar o selo de "disponível para adesão" sem cruzar dados no cliente.

| Campo | Tipo | Descrição |
|---|---|---|
| `situacao` | string | Estado da adesão (ver tabela abaixo). |
| `disponivel` | bool | Selo — `true` **apenas** quando `situacao = "permitida"`. |
| `vigenciaFim` | date-time \| null | Fim da vigência da ata que decide. |
| `diasRestantes` | int \| null | Dias até o fim da vigência (referência: hoje em UTC-3). |
| `numeroControlePncpAta` | string \| null | Ata que decide a disponibilidade. |
| `saldoDisponivel` | decimal \| null | **Sempre `null`** — ver observação. |

**Estados de `situacao`:**

| Valor | Significado | Sugestão de UI |
|---|---|---|
| `permitida` | Ata vigente, não cancelada, que aceita adesão. | Selo verde "Disponível para adesão". |
| `nao_permitida` | Ata vigente, mas o edital não permite adesão. | Não oferecer carona. |
| `nao_informada` | Ata vigente, mas o órgão não informou se aceita adesão. | "Ata vigente — confirmar adesão no edital" + link do PNCP. |
| `sem_ata` | Sem ata vigente para o item, ou item sem fornecedor homologado. | Nada. |

Regras aplicadas:
- Item **sem** resultado homologado (`resultado` vazio) → `sem_ata` (sem fornecedor, não há o que aderir).
- Atas **canceladas** ou com `vigenciaFim` no passado são descartadas.
- Havendo várias atas vigentes, é escolhida **uma única** ata (prioriza a que permite adesão; empate → maior vigência), e todos os campos do bloco vêm dela — evitando falso positivo entre atas diferentes.

> **`saldoDisponivel` é sempre `null` por design.** O limite de adesão de 50% dos quantitativos (art. 86, §3º da Lei 14.133/2021) depende das adesões já realizadas por outros órgãos, e o PNCP não expõe esse dado — logo, o saldo real não é calculável.

> **Estado atual:** enquanto o campo `possibilidade_adesao` não é ingerido do PNCP, `situacao` assume apenas `nao_informada` (item com ata vigente) ou `sem_ata`. O selo `disponivel` ainda não acende. Os estados `permitida`/`nao_permitida` passam a valer quando esse campo for carregado.

---

## Exemplo

**Requisição:**

```
GET /v1/itens-da-compra?descricao=notebook&limit=20
```

**Resposta (`200 OK`):**

```json
{
  "resultado": [
    {
      "orgaoEntidade": {
        "cnpj": "11222333000144",
        "razaoSocial": "ORGAO DE TESTE",
        "poderId": "E",
        "esferaId": "F",
        "unidadeDoOrgao": { "codigoIbge": "3550308" }
      },
      "compra": {
        "anoCompra": 2026,
        "sequencialCompra": 1,
        "numeroCompra": "2026/1",
        "processo": null,
        "objetoCompra": "Aquisicao de notebooks (ata vigente)",
        "ufNome": "Sao Paulo",
        "dataInclusao": "2026-07-24T00:00:00",
        "dataAberturaProposta": null,
        "dataEncerramentoProposta": null,
        "linkSistemaOrigem": "https://pncp.gov.br/app/editais/11222333000144/2026/1",
        "numeroControlePNCP": "11222333000144-1-000001/2026",
        "modalidadeNome": "Pregao - Eletronico",
        "situacaoCompraNome": "Divulgada no PNCP",
        "ufSigla": "SP",
        "atas": [
          {
            "numeroControlePncpAta": "11222333000144-1-000001/2026-000001",
            "numeroAtaRegistroPreco": "ARP-001/2026",
            "anoAta": 2026,
            "objetoContratacao": "Registro de precos de notebooks",
            "cancelada": false,
            "vigenciaInicio": "2026-01-01T00:00:00",
            "vigenciaFim": "2027-06-30T00:00:00"
          }
        ],
        "itemDaCompra": [
          {
            "numeroItem": 1,
            "descricao": "Notebook Dell i7 16GB SSD 512GB",
            "materialOuServico": null,
            "valorUnitarioEstimado": 5000,
            "valorTotal": 50000,
            "quantidade": 10,
            "unidadeMedida": "UN",
            "criterioJulgamentoNome": null,
            "situacaoCompraItemNome": null,
            "resultado": [
              {
                "numeroItem": 1,
                "niFornecedor": "99888777000166",
                "tipoPessoa": null,
                "nomeRazaoSocialFornecedor": "FORNECEDOR A LTDA",
                "porteFornecedorId": null,
                "quantidadeHomologada": 10.0,
                "valorUnitarioHomologado": 4500.0,
                "valorTotalHomologado": 45000.0,
                "ordemClassificacaoSrp": null,
                "numeroControlePNCPCompra": "11222333000144-1-000001/2026"
              }
            ],
            "adesao": {
              "situacao": "nao_informada",
              "disponivel": false,
              "vigenciaFim": "2027-06-30T00:00:00",
              "diasRestantes": 341,
              "numeroControlePncpAta": "11222333000144-1-000001/2026-000001",
              "saldoDisponivel": null
            }
          },
          {
            "numeroItem": 2,
            "descricao": "Notebook Lenovo sem resultado homologado",
            "materialOuServico": null,
            "valorUnitarioEstimado": 4000,
            "valorTotal": 20000,
            "quantidade": 5,
            "unidadeMedida": "UN",
            "criterioJulgamentoNome": null,
            "situacaoCompraItemNome": null,
            "resultado": [],
            "adesao": {
              "situacao": "sem_ata",
              "disponivel": false,
              "vigenciaFim": null,
              "diasRestantes": null,
              "numeroControlePncpAta": null,
              "saldoDisponivel": null
            }
          }
        ]
      }
    }
  ],
  "totalHits": 4,
  "hasMoreItems": false,
  "nextCursor": null
}
```

No exemplo acima há uma compra com ata vigente. Outros dois cenários observados no mesmo teste:

- **Ata vencida:** `atas[]` lista a ata (com `vigenciaFim` no passado), mas o item fica com `adesao.situacao = "sem_ata"`.
- **Compra sem ata:** `atas: []` e `adesao.situacao = "sem_ata"`.

---

## Paginação

Paginação por cursor (offset numérico):

- `limit` define o tamanho da página (padrão `20`).
- `hasMoreItems = true` indica que há mais resultados; use `nextCursor` na próxima chamada:
  ```
  GET /v1/itens-da-compra?descricao=notebook&cursor=20&limit=20
  ```
- `nextCursor = null` quando não há mais páginas.

---

## Códigos de status

| Código | Quando |
|---|---|
| `200 OK` | Busca bem-sucedida (inclusive com zero resultados). |
| `400 Bad Request` | `limit` fora de 1..1000, `cursor` inválido, ou erro na consulta ao Elasticsearch (ex.: índice inexistente). |

---

## Observações e limitações conhecidas

- **`totalHits` não é exato para grandes volumes:** vem do Elasticsearch e satura no limite padrão de contagem (10.000). Trate como "pelo menos N".
- **Filtros de valor não afetam `totalHits`:** são aplicados em memória após a paginação (ver nota na seção de parâmetros).
- **`order`** é aceito na query string, mas ignorado. Com `descricao`, a ordem é por relevância textual; sem `descricao`, por `dataInclusao` decrescente com o `id` como desempate — ordem determinística, necessária para a paginação não repetir itens entre páginas.

### Listar sem termo de busca

Para navegar sem digitar nada (por exemplo, "atas da última semana"), basta omitir `descricao`. Todos os filtros continuam válidos:

```
GET /v1/itens-da-compra?dataInclusaoInicio=2026-08-05&apenasComAdesao=true&limit=20
```

Para trazer apenas o que tem ata vigente com adesão permitida, use `apenasComAdesao=true` — sem esse filtro, a listagem inclui compras sem ata.

### "Quais atas saíram recentemente?"

O período de `dataInclusaoInicio/Fim` é o da **compra**, e a ata costuma ser assinada semanas ou meses depois dela. Para listar itens cujas **atas** foram disponibilizadas num período, filtre pela data da ata:

```
GET /v1/itens-da-compra?apenasComAtaVigente=true&dataDaAtaInicio=2026-07-20&dataDaAtaFim=2026-08-19&limit=50
GET /v1/itens-da-compra?apenasComAdesao=true&dataDaAtaInicio=2026-07-20&ufSigla=GO
GET /v1/itens-da-compra?descricao=limpeza&apenasComAtaVigente=true&dataDaAtaInicio=2026-07-20
```

Sem `descricao`, o resultado vem ordenado pela data da ata (mais recente primeiro); com `descricao`, por relevância. Todos os demais filtros (`ufSigla`, `razaoSocial`, `dataInclusao*`, valores, paginação) continuam valendo.

> **Não filtre adesão/homologação só no cliente quando não houver `descricao`.** Sem termo de busca a ordem é "mais recente primeiro", e compras recém-publicadas quase nunca têm resultado homologado nem ata (isso chega semanas depois). Uma página de 50 itens filtrada no navegador por `adesao.disponivel` tende a ficar vazia mesmo havendo milhares de atas vigentes no período — o filtro precisa ir na query string (`apenasComAdesao`, `apenasComAtaVigente`, `dataDaAta*`) para ser aplicado no Elasticsearch antes da paginação.

> **Atualização dos filtros de ata:** os campos que os sustentam no índice (`ataVigenciaFim`, `ataDataDeReferencia`, `ataAdesaoVigenciaFim`) são recalculados a partir do banco ao fim de **toda** carga (agendada ou manual) e também podem ser refeitos sob demanda com `POST /v1/execucoes` em modo `enriquecimento`. Só entram no cálculo atas não canceladas e vigentes na data do enriquecimento; um item cuja única ata foi cancelada depois mantém os valores antigos até a próxima ata vigente daquela compra (o bloco `adesao`, calculado do banco, não é afetado).
- Campos marcados como "não populado atualmente" existem no contrato, mas hoje retornam `null`.
