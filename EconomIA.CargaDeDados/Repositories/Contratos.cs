using System.Data;
using Dapper;
using EconomIA.CargaDeDados.Models;

namespace EconomIA.CargaDeDados.Repositories;

public class Contratos {
	private readonly IDbConnection conexao;

	public Contratos(IDbConnection conexao) {
		this.conexao = conexao;
	}

	public async Task<long> UpsertAsync(Contrato contrato) {
		var sql = @"
			insert into public.contrato (
				identificador_do_orgao,
				numero_controle_pncp,
				numero_controle_pncp_compra,
				ano_contrato,
				sequencial_contrato,
				numero_contrato_empenho,
				processo,
				objeto_contrato,
				tipo_contrato_id,
				tipo_contrato_nome,
				categoria_processo_id,
				categoria_processo_nome,
				ni_fornecedor,
				nome_razao_social_fornecedor,
				tipo_pessoa,
				valor_inicial,
				valor_global,
				valor_parcela,
				valor_acumulado,
				numero_parcelas,
				data_assinatura,
				data_vigencia_inicio,
				data_vigencia_fim,
				data_publicacao_pncp,
				data_atualizacao,
				data_atualizacao_global,
				receita,
				informacao_complementar,
				usuario_nome,
				atualizado_em
			) values (
				@IdentificadorDoOrgao,
				@NumeroControlePncp,
				@NumeroControlePncpCompra,
				@AnoContrato,
				@SequencialContrato,
				@NumeroContratoEmpenho,
				@Processo,
				@ObjetoContrato,
				@TipoContratoId,
				@TipoContratoNome,
				@CategoriaProcessoId,
				@CategoriaProcessoNome,
				@NiFornecedor,
				@NomeRazaoSocialFornecedor,
				@TipoPessoa,
				@ValorInicial,
				@ValorGlobal,
				@ValorParcela,
				@ValorAcumulado,
				@NumeroParcelas,
				@DataAssinatura,
				@DataVigenciaInicio,
				@DataVigenciaFim,
				@DataPublicacaoPncp,
				@DataAtualizacao,
				@DataAtualizacaoGlobal,
				@Receita,
				@InformacaoComplementar,
				@UsuarioNome,
				now()
			)
			on conflict (numero_controle_pncp) do update
			set
				numero_controle_pncp_compra = excluded.numero_controle_pncp_compra,
				numero_contrato_empenho = excluded.numero_contrato_empenho,
				processo = excluded.processo,
				objeto_contrato = excluded.objeto_contrato,
				tipo_contrato_id = excluded.tipo_contrato_id,
				tipo_contrato_nome = excluded.tipo_contrato_nome,
				categoria_processo_id = excluded.categoria_processo_id,
				categoria_processo_nome = excluded.categoria_processo_nome,
				ni_fornecedor = excluded.ni_fornecedor,
				nome_razao_social_fornecedor = excluded.nome_razao_social_fornecedor,
				tipo_pessoa = excluded.tipo_pessoa,
				valor_inicial = excluded.valor_inicial,
				valor_global = excluded.valor_global,
				valor_parcela = excluded.valor_parcela,
				valor_acumulado = excluded.valor_acumulado,
				numero_parcelas = excluded.numero_parcelas,
				data_assinatura = excluded.data_assinatura,
				data_vigencia_inicio = excluded.data_vigencia_inicio,
				data_vigencia_fim = excluded.data_vigencia_fim,
				data_publicacao_pncp = excluded.data_publicacao_pncp,
				data_atualizacao = excluded.data_atualizacao,
				data_atualizacao_global = excluded.data_atualizacao_global,
				receita = excluded.receita,
				informacao_complementar = excluded.informacao_complementar,
				usuario_nome = excluded.usuario_nome,
				atualizado_em = now()
			returning identificador;
		";

		return await conexao.ExecuteScalarAsync<long>(sql, contrato);
	}
}
