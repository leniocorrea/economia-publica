-- Migration: Adicionar modo 'brasil' na constraint de modo_execucao
-- Data: 2026-01-17

-- Atualizar constraint para incluir modo 'brasil' na tabela execucao_carga
ALTER TABLE public.execucao_carga DROP CONSTRAINT IF EXISTS chk_modo_execucao;
ALTER TABLE public.execucao_carga ADD CONSTRAINT chk_modo_execucao
    CHECK (modo_execucao IN ('diaria', 'incremental', 'manual', 'orgaos', 'brasil'));

-- Atualizar constraint de status para incluir 'pendente' (usado pelo ExecucaoManualWorker)
ALTER TABLE public.execucao_carga DROP CONSTRAINT IF EXISTS chk_status_execucao;
ALTER TABLE public.execucao_carga ADD CONSTRAINT chk_status_execucao
    CHECK (status IN ('pendente', 'em_andamento', 'sucesso', 'erro', 'cancelado', 'parcial'));

-- Adicionar coluna de parametros se nao existir (usada para execucao manual)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
        AND table_name = 'execucao_carga'
        AND column_name = 'parametros'
    ) THEN
        ALTER TABLE public.execucao_carga ADD COLUMN parametros JSONB;
    END IF;
END $$;
