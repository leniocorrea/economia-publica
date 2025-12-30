using System.Data;
using System.Text;
using Dapper;
using EconomIA.CargaDeDados.Models;
using Npgsql;

namespace EconomIA.CargaDeDados.Repositories;

public class Unidades {
	private readonly IDbConnection conexao;

	public Unidades(IDbConnection conexao) {
		this.conexao = conexao;
	}

	/// <summary>
	/// Bulk insert usando COPY do PostgreSQL (muito mais rápido que INSERT individual)
	/// Usa tabela temporária + INSERT ON CONFLICT para fazer upsert
	/// </summary>
	public async Task<int> BulkUpsertAsync(IEnumerable<Unidade> listaUnidades) {
		var lista = listaUnidades.ToList();
		if (lista.Count == 0) {
			return 0;
		}

		var npgsqlConnection = conexao as NpgsqlConnection;
		if (npgsqlConnection is null) {
			return await UpsertEmLoteAsync(lista);
		}

		if (npgsqlConnection.State != ConnectionState.Open) {
			await npgsqlConnection.OpenAsync();
		}

		// Usar transação para manter a tabela temporária viva
		await using var transacao = await npgsqlConnection.BeginTransactionAsync();

		try {
			// Criar tabela temporária (sem ON COMMIT DROP para funcionar com COPY)
			await conexao.ExecuteAsync(@"
				create temp table if not exists temp_unidade_bulk (
					identificador_do_orgao bigint,
					codigo_unidade text,
					nome_unidade text,
					municipio_nome text,
					municipio_codigo_ibge text,
					uf_sigla text,
					uf_nome text,
					status_ativo boolean,
					data_inclusao_pncp timestamp,
					data_atualizacao_pncp timestamp,
					justificativa_atualizacao text
				)", transaction: transacao);

			// Limpar tabela temporária antes de usar
			await conexao.ExecuteAsync("truncate temp_unidade_bulk", transaction: transacao);

			// COPY para tabela temporária
			await using (var writer = await npgsqlConnection.BeginBinaryImportAsync(
				"copy temp_unidade_bulk (identificador_do_orgao, codigo_unidade, nome_unidade, municipio_nome, municipio_codigo_ibge, uf_sigla, uf_nome, status_ativo, data_inclusao_pncp, data_atualizacao_pncp, justificativa_atualizacao) from stdin (format binary)")) {
				foreach (var unidade in lista) {
					await writer.StartRowAsync();
					await writer.WriteAsync(unidade.IdentificadorDoOrgao, NpgsqlTypes.NpgsqlDbType.Bigint);
					await writer.WriteAsync(unidade.CodigoUnidade ?? (object)DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
					await writer.WriteAsync(unidade.NomeUnidade ?? (object)DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
					await writer.WriteAsync(unidade.MunicipioNome ?? (object)DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
					await writer.WriteAsync(unidade.MunicipioCodigoIbge ?? (object)DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
					await writer.WriteAsync(unidade.UfSigla ?? (object)DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
					await writer.WriteAsync(unidade.UfNome ?? (object)DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
					await writer.WriteAsync(unidade.StatusAtivo, NpgsqlTypes.NpgsqlDbType.Boolean);
					await writer.WriteAsync(unidade.DataInclusaoPncp ?? (object)DBNull.Value, NpgsqlTypes.NpgsqlDbType.Timestamp);
					await writer.WriteAsync(unidade.DataAtualizacaoPncp ?? (object)DBNull.Value, NpgsqlTypes.NpgsqlDbType.Timestamp);
					await writer.WriteAsync(unidade.JustificativaAtualizacao ?? (object)DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
				}
				await writer.CompleteAsync();
			}

			// Upsert da temporária para a definitiva
			var inseridos = await conexao.ExecuteAsync(@"
				insert into public.unidade (
					identificador_do_orgao, codigo_unidade, nome_unidade,
					municipio_nome, municipio_codigo_ibge, uf_sigla, uf_nome,
					status_ativo, data_inclusao_pncp, data_atualizacao_pncp,
					justificativa_atualizacao, atualizado_em
				)
				select
					identificador_do_orgao, codigo_unidade, nome_unidade,
					municipio_nome, municipio_codigo_ibge, uf_sigla, uf_nome,
					status_ativo, data_inclusao_pncp, data_atualizacao_pncp,
					justificativa_atualizacao, now()
				from temp_unidade_bulk
				on conflict (identificador_do_orgao, codigo_unidade) do update
				set nome_unidade = excluded.nome_unidade,
					municipio_nome = excluded.municipio_nome,
					municipio_codigo_ibge = excluded.municipio_codigo_ibge,
					uf_sigla = excluded.uf_sigla,
					uf_nome = excluded.uf_nome,
					status_ativo = excluded.status_ativo,
					data_inclusao_pncp = excluded.data_inclusao_pncp,
					data_atualizacao_pncp = excluded.data_atualizacao_pncp,
					justificativa_atualizacao = excluded.justificativa_atualizacao,
					atualizado_em = now()", transaction: transacao);

			await transacao.CommitAsync();
			return inseridos;
		} catch {
			await transacao.RollbackAsync();
			throw;
		}
	}

	public async Task<long> UpsertAsync(Unidade unidade) {
		var sql = @"
			insert into public.unidade (
				identificador_do_orgao, codigo_unidade, nome_unidade,
				municipio_nome, municipio_codigo_ibge, uf_sigla, uf_nome,
				status_ativo, data_inclusao_pncp, data_atualizacao_pncp,
				justificativa_atualizacao, atualizado_em
			)
			values (
				@IdentificadorDoOrgao, @CodigoUnidade, @NomeUnidade,
				@MunicipioNome, @MunicipioCodigoIbge, @UfSigla, @UfNome,
				@StatusAtivo, @DataInclusaoPncp, @DataAtualizacaoPncp,
				@JustificativaAtualizacao, now()
			)
			on conflict (identificador_do_orgao, codigo_unidade) do update
			set nome_unidade = excluded.nome_unidade,
				municipio_nome = excluded.municipio_nome,
				municipio_codigo_ibge = excluded.municipio_codigo_ibge,
				uf_sigla = excluded.uf_sigla,
				uf_nome = excluded.uf_nome,
				status_ativo = excluded.status_ativo,
				data_inclusao_pncp = excluded.data_inclusao_pncp,
				data_atualizacao_pncp = excluded.data_atualizacao_pncp,
				justificativa_atualizacao = excluded.justificativa_atualizacao,
				atualizado_em = now()
			returning identificador;
		";

		return await conexao.ExecuteScalarAsync<long>(sql, unidade);
	}

	public async Task<int> UpsertEmLoteAsync(IEnumerable<Unidade> listaUnidades) {
		var sql = @"
			insert into public.unidade (
				identificador_do_orgao, codigo_unidade, nome_unidade,
				municipio_nome, municipio_codigo_ibge, uf_sigla, uf_nome,
				status_ativo, data_inclusao_pncp, data_atualizacao_pncp,
				justificativa_atualizacao, atualizado_em
			)
			values (
				@IdentificadorDoOrgao, @CodigoUnidade, @NomeUnidade,
				@MunicipioNome, @MunicipioCodigoIbge, @UfSigla, @UfNome,
				@StatusAtivo, @DataInclusaoPncp, @DataAtualizacaoPncp,
				@JustificativaAtualizacao, now()
			)
			on conflict (identificador_do_orgao, codigo_unidade) do update
			set nome_unidade = excluded.nome_unidade,
				municipio_nome = excluded.municipio_nome,
				municipio_codigo_ibge = excluded.municipio_codigo_ibge,
				uf_sigla = excluded.uf_sigla,
				uf_nome = excluded.uf_nome,
				status_ativo = excluded.status_ativo,
				data_inclusao_pncp = excluded.data_inclusao_pncp,
				data_atualizacao_pncp = excluded.data_atualizacao_pncp,
				justificativa_atualizacao = excluded.justificativa_atualizacao,
				atualizado_em = now();
		";

		return await conexao.ExecuteAsync(sql, listaUnidades);
	}
}
