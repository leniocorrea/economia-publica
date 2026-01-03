-- Migration: Criar tabelas de historico de execucao de carga
-- Data: 2026-01-03

-- Tabela: execucao_carga
-- Armazena o historico de execucoes do worker de carga
CREATE TABLE IF NOT EXISTS public.execucao_carga (
    identificador BIGSERIAL PRIMARY KEY,
    modo_execucao VARCHAR(20) NOT NULL,
    tipo_gatilho VARCHAR(20) NOT NULL,
    inicio_em TIMESTAMP WITH TIME ZONE NOT NULL,
    fim_em TIMESTAMP WITH TIME ZONE,
    duracao_total_ms BIGINT,
    status VARCHAR(20) NOT NULL DEFAULT 'em_andamento',
    mensagem_erro TEXT,
    stack_trace TEXT,

    -- Metricas agregadas
    total_orgaos_processados INTEGER DEFAULT 0,
    total_orgaos_com_erro INTEGER DEFAULT 0,
    total_compras_processadas INTEGER DEFAULT 0,
    total_contratos_processados INTEGER DEFAULT 0,
    total_atas_processadas INTEGER DEFAULT 0,
    total_itens_indexados INTEGER DEFAULT 0,

    -- Contexto
    versao_aplicacao VARCHAR(20),
    hostname VARCHAR(100),
    criado_em TIMESTAMP WITH TIME ZONE DEFAULT NOW(),

    CONSTRAINT chk_modo_execucao CHECK (modo_execucao IN ('diaria', 'incremental', 'manual', 'orgaos')),
    CONSTRAINT chk_tipo_gatilho CHECK (tipo_gatilho IN ('scheduler', 'cli', 'api')),
    CONSTRAINT chk_status_execucao CHECK (status IN ('em_andamento', 'sucesso', 'erro', 'cancelado', 'parcial'))
);

CREATE INDEX IF NOT EXISTS idx_execucao_carga_status ON public.execucao_carga (status);
CREATE INDEX IF NOT EXISTS idx_execucao_carga_inicio ON public.execucao_carga (inicio_em DESC);
CREATE INDEX IF NOT EXISTS idx_execucao_carga_modo ON public.execucao_carga (modo_execucao);

-- Tabela: execucao_carga_orgao
-- Armazena detalhes de execucao por orgao
CREATE TABLE IF NOT EXISTS public.execucao_carga_orgao (
    identificador BIGSERIAL PRIMARY KEY,
    identificador_da_execucao BIGINT NOT NULL REFERENCES public.execucao_carga(identificador) ON DELETE CASCADE,
    identificador_do_orgao BIGINT NOT NULL REFERENCES public.orgao(identificador),
    inicio_em TIMESTAMP WITH TIME ZONE NOT NULL,
    fim_em TIMESTAMP WITH TIME ZONE,
    duracao_ms BIGINT,
    status VARCHAR(20) NOT NULL DEFAULT 'em_andamento',
    mensagem_erro TEXT,

    -- Metricas por tipo
    compras_processadas INTEGER DEFAULT 0,
    compras_duracao_ms BIGINT DEFAULT 0,
    contratos_processados INTEGER DEFAULT 0,
    contratos_duracao_ms BIGINT DEFAULT 0,
    atas_processadas INTEGER DEFAULT 0,
    atas_duracao_ms BIGINT DEFAULT 0,
    itens_processados INTEGER DEFAULT 0,

    data_inicial_processada DATE,
    data_final_processada DATE,
    criado_em TIMESTAMP WITH TIME ZONE DEFAULT NOW(),

    CONSTRAINT chk_status_execucao_orgao CHECK (status IN ('em_andamento', 'sucesso', 'erro'))
);

CREATE INDEX IF NOT EXISTS idx_execucao_carga_orgao_execucao ON public.execucao_carga_orgao (identificador_da_execucao);
CREATE INDEX IF NOT EXISTS idx_execucao_carga_orgao_orgao ON public.execucao_carga_orgao (identificador_do_orgao);
CREATE INDEX IF NOT EXISTS idx_execucao_carga_orgao_status ON public.execucao_carga_orgao (status);
