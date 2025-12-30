using System.Data;
using Dapper;
using EconomIA.CargaDeDados.Models;

namespace EconomIA.CargaDeDados.Repositories;

public class ItensDaCompra {
	private readonly IDbConnection conexao;

	public ItensDaCompra(IDbConnection conexao) {
		this.conexao = conexao;
	}

	public async Task<long> UpsertAsync(ItemDaCompra item) {
		var sql = @"
			insert into public.item_da_compra (
				identificador_da_compra,
				numero_item,
				descricao,
				quantidade,
				unidade_medida,
				valor_unitario_estimado,
				valor_total,
				criterio_julgamento_nome,
				situacao_compra_item_nome,
				tem_resultado,
				data_atualizacao,
				atualizado_em
			) values (
				@IdentificadorDaCompra,
				@NumeroItem,
				@Descricao,
				@Quantidade,
				@UnidadeMedida,
				@ValorUnitarioEstimado,
				@ValorTotal,
				@CriterioJulgamentoNome,
				@SituacaoCompraItemNome,
				@TemResultado,
				now(),
				now()
			)
			on conflict (identificador_da_compra, numero_item) do update
			set
				descricao = excluded.descricao,
				quantidade = excluded.quantidade,
				unidade_medida = excluded.unidade_medida,
				valor_unitario_estimado = excluded.valor_unitario_estimado,
				valor_total = excluded.valor_total,
				criterio_julgamento_nome = excluded.criterio_julgamento_nome,
				situacao_compra_item_nome = excluded.situacao_compra_item_nome,
				tem_resultado = excluded.tem_resultado,
				data_atualizacao = now(),
				atualizado_em = now()
			returning identificador;
		";

		return await conexao.ExecuteScalarAsync<long>(sql, item);
	}
}
