-- Extensoes
CREATE EXTENSION IF NOT EXISTS vector;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Tabela: orgao
CREATE TABLE IF NOT EXISTS public.orgao (
    identificador BIGSERIAL PRIMARY KEY,
    cnpj VARCHAR(20) NOT NULL,
    razao_social VARCHAR(500) NOT NULL,
    nome_fantasia VARCHAR(500),
    codigo_natureza_juridica VARCHAR(10),
    descricao_natureza_juridica VARCHAR(500),
    poder_id VARCHAR(5),
    esfera_id VARCHAR(5),
    situacao_cadastral VARCHAR(50),
    motivo_situacao_cadastral VARCHAR(500),
    data_situacao_cadastral TIMESTAMP,
    data_validacao TIMESTAMP,
    validado BOOLEAN DEFAULT false,
    data_inclusao_pncp TIMESTAMP,
    data_atualizacao_pncp TIMESTAMP,
    status_ativo BOOLEAN DEFAULT true,
    justificativa_atualizacao VARCHAR(1000),
    criado_em TIMESTAMP NOT NULL DEFAULT now(),
    atualizado_em TIMESTAMP NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_orgao_cnpj ON public.orgao (cnpj);

-- Tabela: unidade
CREATE TABLE IF NOT EXISTS public.unidade (
    identificador BIGSERIAL PRIMARY KEY,
    identificador_do_orgao BIGINT NOT NULL REFERENCES public.orgao(identificador),
    codigo_unidade VARCHAR(50) NOT NULL,
    nome_unidade VARCHAR(500) NOT NULL,
    municipio_nome VARCHAR(200),
    municipio_codigo_ibge VARCHAR(10),
    uf_sigla VARCHAR(2),
    uf_nome VARCHAR(50),
    status_ativo BOOLEAN DEFAULT true,
    data_inclusao_pncp TIMESTAMP,
    data_atualizacao_pncp TIMESTAMP,
    justificativa_atualizacao VARCHAR(1000),
    criado_em TIMESTAMP NOT NULL DEFAULT now(),
    atualizado_em TIMESTAMP NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_unidade_orgao_codigo ON public.unidade (identificador_do_orgao, codigo_unidade);
CREATE INDEX IF NOT EXISTS idx_unidade_municipio ON public.unidade (municipio_codigo_ibge);
CREATE INDEX IF NOT EXISTS idx_unidade_uf ON public.unidade (uf_sigla);

-- Tabela: compra
CREATE TABLE IF NOT EXISTS public.compra (
    identificador BIGSERIAL PRIMARY KEY,
    identificador_do_orgao BIGINT NOT NULL REFERENCES public.orgao(identificador),
    numero_controle_pncp VARCHAR(100) NOT NULL,
    ano_compra INTEGER NOT NULL,
    sequencial_compra INTEGER NOT NULL,
    modalidade_identificador INTEGER NOT NULL,
    modalidade_nome VARCHAR(200),
    objeto_compra TEXT,
    valor_total_estimado NUMERIC(18,2),
    valor_total_homologado NUMERIC(18,2),
    situacao_compra_nome VARCHAR(200),
    data_abertura_proposta TIMESTAMP,
    data_encerramento_proposta TIMESTAMP,
    amparo_legal_nome VARCHAR(500),
    modo_disputa_nome VARCHAR(200),
    link_pncp VARCHAR(500),
    data_atualizacao_global TIMESTAMP,
    itens_carregados BOOLEAN NOT NULL DEFAULT false,
    criado_em TIMESTAMP NOT NULL DEFAULT now(),
    atualizado_em TIMESTAMP NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_compra_unique ON public.compra (identificador_do_orgao, ano_compra, sequencial_compra);

-- Tabela: item_da_compra
CREATE TABLE IF NOT EXISTS public.item_da_compra (
    identificador BIGSERIAL PRIMARY KEY,
    identificador_da_compra BIGINT NOT NULL REFERENCES public.compra(identificador),
    numero_item INTEGER NOT NULL,
    descricao TEXT,
    quantidade NUMERIC(18,4),
    unidade_medida VARCHAR(100),
    valor_unitario_estimado NUMERIC(18,4),
    valor_total NUMERIC(18,2),
    criterio_julgamento_nome VARCHAR(200),
    situacao_compra_item_nome VARCHAR(200),
    tem_resultado BOOLEAN NOT NULL DEFAULT false,
    data_atualizacao TIMESTAMP,
    criado_em TIMESTAMP NOT NULL DEFAULT now(),
    atualizado_em TIMESTAMP NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_item_compra_unique ON public.item_da_compra (identificador_da_compra, numero_item);

-- Tabela: resultado_do_item
CREATE TABLE IF NOT EXISTS public.resultado_do_item (
    identificador BIGSERIAL PRIMARY KEY,
    identificador_do_item_da_compra BIGINT NOT NULL REFERENCES public.item_da_compra(identificador),
    ni_fornecedor VARCHAR(50),
    nome_razao_social_fornecedor VARCHAR(500),
    quantidade_homologada NUMERIC(18,4),
    valor_unitario_homologado NUMERIC(18,4),
    valor_total_homologado NUMERIC(18,2),
    situacao_compra_item_resultado_nome VARCHAR(200),
    data_resultado TIMESTAMP,
    data_atualizacao TIMESTAMP,
    criado_em TIMESTAMP NOT NULL DEFAULT now(),
    atualizado_em TIMESTAMP NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_resultado_item_unique ON public.resultado_do_item (identificador_do_item_da_compra, ni_fornecedor);

-- Tabela: contrato
CREATE TABLE IF NOT EXISTS public.contrato (
    identificador BIGSERIAL PRIMARY KEY,
    identificador_do_orgao BIGINT NOT NULL REFERENCES public.orgao(identificador),
    numero_controle_pncp VARCHAR(100) NOT NULL UNIQUE,
    numero_controle_pncp_compra VARCHAR(100),
    ano_contrato INTEGER NOT NULL,
    sequencial_contrato INTEGER NOT NULL,
    numero_contrato_empenho VARCHAR(200),
    processo VARCHAR(200),
    objeto_contrato TEXT,
    tipo_contrato_id INTEGER,
    tipo_contrato_nome VARCHAR(200),
    categoria_processo_id INTEGER,
    categoria_processo_nome VARCHAR(200),
    ni_fornecedor VARCHAR(20),
    nome_razao_social_fornecedor VARCHAR(500),
    tipo_pessoa VARCHAR(10),
    valor_inicial NUMERIC(18,4),
    valor_global NUMERIC(18,4),
    valor_parcela NUMERIC(18,4),
    valor_acumulado NUMERIC(18,4),
    numero_parcelas INTEGER,
    data_assinatura DATE,
    data_vigencia_inicio DATE,
    data_vigencia_fim DATE,
    data_publicacao_pncp TIMESTAMP,
    data_atualizacao TIMESTAMP,
    data_atualizacao_global TIMESTAMP,
    receita BOOLEAN DEFAULT false,
    informacao_complementar TEXT,
    usuario_nome VARCHAR(200),
    criado_em TIMESTAMP DEFAULT now(),
    atualizado_em TIMESTAMP DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_contrato_orgao ON public.contrato (identificador_do_orgao);
CREATE INDEX IF NOT EXISTS idx_contrato_ano ON public.contrato (ano_contrato);
CREATE INDEX IF NOT EXISTS idx_contrato_fornecedor ON public.contrato (ni_fornecedor);

-- Tabela: ata
CREATE TABLE IF NOT EXISTS public.ata (
    identificador BIGSERIAL PRIMARY KEY,
    identificador_do_orgao BIGINT NOT NULL REFERENCES public.orgao(identificador),
    numero_controle_pncp_ata VARCHAR(100) NOT NULL UNIQUE,
    numero_controle_pncp_compra VARCHAR(100),
    numero_ata_registro_preco VARCHAR(100),
    ano_ata INTEGER NOT NULL,
    objeto_contratacao TEXT,
    cancelado BOOLEAN DEFAULT false,
    data_cancelamento TIMESTAMP,
    data_assinatura DATE,
    vigencia_inicio DATE,
    vigencia_fim DATE,
    data_publicacao_pncp TIMESTAMP,
    data_inclusao TIMESTAMP,
    data_atualizacao TIMESTAMP,
    data_atualizacao_global TIMESTAMP,
    usuario VARCHAR(200),
    criado_em TIMESTAMP DEFAULT now(),
    atualizado_em TIMESTAMP DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_ata_orgao ON public.ata (identificador_do_orgao);
CREATE INDEX IF NOT EXISTS idx_ata_ano ON public.ata (ano_ata);
CREATE INDEX IF NOT EXISTS idx_ata_compra ON public.ata (numero_controle_pncp_compra);

-- Tabela: controle_de_importacao
CREATE TABLE IF NOT EXISTS public.controle_de_importacao (
    identificador BIGSERIAL PRIMARY KEY,
    identificador_do_orgao BIGINT NOT NULL REFERENCES public.orgao(identificador),
    tipo_dado VARCHAR(50) NOT NULL,
    data_inicial_importada DATE,
    data_final_importada DATE,
    ultima_execucao TIMESTAMP NOT NULL DEFAULT now(),
    registros_importados INTEGER NOT NULL DEFAULT 0,
    status VARCHAR(20) NOT NULL DEFAULT 'pendente',
    mensagem_erro TEXT,
    criado_em TIMESTAMP NOT NULL DEFAULT now(),
    atualizado_em TIMESTAMP NOT NULL DEFAULT now(),
    CONSTRAINT chk_tipo_dado CHECK (tipo_dado IN ('compras', 'itens_compra', 'resultados_itens', 'contratos', 'atas')),
    CONSTRAINT chk_status CHECK (status IN ('pendente', 'em_andamento', 'sucesso', 'erro'))
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_controle_orgao_tipo ON public.controle_de_importacao (identificador_do_orgao, tipo_dado);
CREATE INDEX IF NOT EXISTS idx_controle_importacao_orgao ON public.controle_de_importacao (identificador_do_orgao);
CREATE INDEX IF NOT EXISTS idx_controle_importacao_status ON public.controle_de_importacao (status);
