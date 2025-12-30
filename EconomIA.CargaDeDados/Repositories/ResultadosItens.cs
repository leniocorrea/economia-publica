using System.Data;
using Dapper;
using EconomIA.CargaDeDados.Models;

namespace EconomIA.CargaDeDados.Repositories;

public class ResultadosItens {
	private readonly IDbConnection conexao;

	public ResultadosItens(IDbConnection conexao) {
		this.conexao = conexao;
	}

	public async Task UpsertAsync(ResultadoItem resultado) {
		var sql = @"
			insert into public.resultado_do_item (
				identificador_do_item_da_compra,
				ni_fornecedor,
				nome_razao_social_fornecedor,
				quantidade_homologada,
				valor_unitario_homologado,
				valor_total_homologado,
				situacao_compra_item_resultado_nome,
				data_resultado,
				data_atualizacao,
				atualizado_em
			) values (
				@IdentificadorDoItemDaCompra,
				@NiFornecedor,
				@NomeRazaoSocialFornecedor,
				@QuantidadeHomologada,
				@ValorUnitarioHomologado,
				@ValorTotalHomologado,
				@SituacaoCompraItemResultadoNome,
				@DataResultado,
				now(),
				now()
			)
			on conflict (identificador_do_item_da_compra, ni_fornecedor) do update
			set
				nome_razao_social_fornecedor = excluded.nome_razao_social_fornecedor,
				quantidade_homologada = excluded.quantidade_homologada,
				valor_unitario_homologado = excluded.valor_unitario_homologado,
				valor_total_homologado = excluded.valor_total_homologado,
				situacao_compra_item_resultado_nome = excluded.situacao_compra_item_resultado_nome,
				data_resultado = excluded.data_resultado,
				data_atualizacao = now(),
				atualizado_em = now();
		";

		await conexao.ExecuteAsync(sql, resultado);
	}
}
