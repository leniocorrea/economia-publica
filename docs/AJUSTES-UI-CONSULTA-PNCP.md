# Ajustes na UI — Consulta PNCP (atas recentes e adesão)

**Endpoint:** `GET /v1/itens-da-compra` (o mesmo). **Nada foi removido**: a tela atual continua funcionando; o que muda é *como* ela deve pedir os dados.

---

## 1. O problema que o usuário viu

| Como a tela faz hoje | Consequência |
|---|---|
| 1 requisição por UF (27×), `limit=50`, sem `descricao` | Sem texto a API ordena "compra mais recente primeiro" → os 50 de cada UF são compras publicadas ontem, que ainda **não têm resultado homologado nem ata** |
| "Somente Homologados" e "Adesão Permitida" filtrados **no navegador** sobre esses 50 | Tudo cai no filtro → "Nenhum resultado" — mesmo havendo milhares de atas vigentes no período |
| Período da tela = `dataInclusaoInicio/Fim` (data da **compra**) | A ata é assinada semanas/meses depois da compra. "Atas do último mês" não é "compras do último mês" |

Com "limpeza" digitado a relevância trouxe, por acaso, uma compra antiga que já tinha ata — por isso "achou uma".

---

## 2. As três alterações

### 2.1 "Adesão Permitida? (Carona)" → parâmetro, não filtro de cliente
- **Sim** → acrescentar `apenasComAdesao=true` na query string e **remover** o filtro por `adesao.disponivel` no navegador.
- **Não / vazio** → não enviar o parâmetro (`false` é igual a omitir; não existe "só sem adesão").

### 2.2 Período "do último mês" deve ser da **ata**, não da compra
Novos parâmetros, todos opcionais e combináveis com os demais (`ufSigla`, `razaoSocial`, `descricao`, valores, `limit`, `cursor`):

| Parâmetro | Quando usar |
|---|---|
| `apenasComAtaVigente=true` | "quero só o que tem ata vigente hoje" (permitindo carona ou não) |
| `dataDaAtaInicio=YYYY-MM-DD` / `dataDaAtaFim=YYYY-MM-DD` | período pela **data de referência da ata** (assinatura; na falta, publicação). `dataDaAtaFim` inclui o dia inteiro. Início > fim devolve `400`. |

Sugestão de UI (mínima): quando "Adesão Permitida = Sim" **ou** um novo toggle "Somente com ata vigente" estiver ligado, enviar os campos Data Início/Data Fim da tela como `dataDaAtaInicio/Fim` (em vez de `dataInclusaoInicio/Fim`) e rotular o bloco como "Período da ata". Caso contrário, manter `dataInclusaoInicio/Fim` como hoje.

### 2.3 "Somente Homologados?"
- Com `apenasComAdesao=true` ou `apenasComAtaVigente=true`, **já vem só item com resultado homologado** (regra do servidor) — nada a fazer.
- Sem esses filtros, continua sendo filtro de cliente (não há parâmetro server-side hoje); deixar claro na UI que ele só vale sobre a página carregada.

---

## 3. Como a API se comporta com os filtros novos

- **Com `descricao`**: busca textual por relevância, como sempre; os filtros de ata só restringem o conjunto.
- **Sem `descricao` + qualquer filtro de ata**: ordem = **data da ata decrescente** (mais recente primeiro). Sem filtro de ata continua `dataInclusao` decrescente.
- `totalHits`, `hasMoreItems` e `nextCursor` refletem o filtro (paginação de verdade; com "Todos" os estados dá para fazer **uma** requisição sem `ufSigla` e paginar, em vez de 27 — opcional).
- Os blocos já usados pela tela (`itemDaCompra[].adesao`, `compra.atas[]`) não mudaram.
- Não exija mais "mínimo 4 caracteres" na descrição: busca sem texto é suportada.

### Exemplos
```
# cenário do usuário: último mês, todas as UFs, adesão permitida, sem texto (uma chamada por UF ou sem ufSigla)
GET /v1/itens-da-compra?apenasComAdesao=true&dataDaAtaInicio=2026-07-20&dataDaAtaFim=2026-08-19&ufSigla=GO&limit=50

# mesma coisa com texto
GET /v1/itens-da-compra?descricao=limpeza&apenasComAdesao=true&dataDaAtaInicio=2026-07-20&dataDaAtaFim=2026-08-19&limit=50

# toda ata vigente assinada desde 20/07, permitindo carona ou não
GET /v1/itens-da-compra?apenasComAtaVigente=true&dataDaAtaInicio=2026-07-20&limit=50
```

---

## 4. Checklist de validação

1. Período 20/07–19/08, "Todos" os estados, descrição vazia, Adesão = Sim → a tela lista itens (ordem: ata mais recente primeiro) e o modal mostra `Adesão Permitida (Carona)` com o Nº de controle da ata.
2. Mesmo cenário com "limpeza" → continua listando (agora com a compra `01181585000156-1-000167/2026` entre elas).
3. Adesão = Não → não envia `apenasComAdesao`; com "Somente com ata vigente" ligado aparecem também itens `nao_informada`/`nao_permitida`.
4. `dataDaAtaInicio` > `dataDaAtaFim` → a API devolve `400`; tratar na tela antes de enviar.
5. Paginação: "carregar mais" continua usando `nextCursor` por UF (ou global, se migrar para requisição única).

> Os campos que sustentam esses filtros no índice são recalculados ao fim de cada carga diária. Se a API voltar vazio logo após o deploy, é só aguardar a execução de enriquecimento (o back-end dispara uma assim que a versão nova subir).
