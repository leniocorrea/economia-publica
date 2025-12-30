using System.Data;
using Dapper;
using EconomIA.CargaDeDados.Models;

namespace EconomIA.CargaDeDados.Repositories;

public class Compras {
	private readonly IDbConnection conexao;

	public Compras(IDbConnection conexao) {
		this.conexao = conexao;
	}

	public async Task<long> UpsertAsync(Compra compra) {
		var sql = @"
			insert into public.compra (
				identificador_do_orgao,
				numero_controle_pncp,
				ano_compra,
				sequencial_compra,
				modalidade_identificador,
				modalidade_nome,
				objeto_compra,
				valor_total_estimado,
				valor_total_homologado,
				situacao_compra_nome,
				data_abertura_proposta,
				data_encerramento_proposta,
				amparo_legal_nome,
				modo_disputa_nome,
				link_pncp,
				data_atualizacao_global,
				itens_carregados,
				atualizado_em
			) values (
				@IdentificadorDoOrgao,
				@NumeroControlePncp,
				@AnoCompra,
				@SequencialCompra,
				@ModalidadeIdentificador,
				@ModalidadeNome,
				@ObjetoCompra,
				@ValorTotalEstimado,
				@ValorTotalHomologado,
				@SituacaoCompraNome,
				@DataAberturaProposta,
				@DataEncerramentoProposta,
				@AmparoLegalNome,
				@ModoDisputaNome,
				@LinkPncp,
				@DataAtualizacaoGlobal,
				@ItensCarregados,
				now()
			)
			on conflict (identificador_do_orgao, ano_compra, sequencial_compra) do update
			set
				numero_controle_pncp = excluded.numero_controle_pncp,
				modalidade_identificador = excluded.modalidade_identificador,
				modalidade_nome = excluded.modalidade_nome,
				objeto_compra = excluded.objeto_compra,
				valor_total_estimado = excluded.valor_total_estimado,
				valor_total_homologado = excluded.valor_total_homologado,
				situacao_compra_nome = excluded.situacao_compra_nome,
				data_abertura_proposta = excluded.data_abertura_proposta,
				data_encerramento_proposta = excluded.data_encerramento_proposta,
				amparo_legal_nome = excluded.amparo_legal_nome,
				modo_disputa_nome = excluded.modo_disputa_nome,
				link_pncp = excluded.link_pncp,
				data_atualizacao_global = excluded.data_atualizacao_global,
				atualizado_em = now()
			returning identificador;
		";

		return await conexao.ExecuteScalarAsync<long>(sql, compra);
	}

	public async Task AtualizarStatusItensCarregadosAsync(long idCompra, bool carregado) {
		var sql = "update public.compra set itens_carregados = @Carregado where identificador = @Id";
		await conexao.ExecuteAsync(sql, new { Id = idCompra, Carregado = carregado });
	}
}
