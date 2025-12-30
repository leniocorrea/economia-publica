using System.Data;
using Dapper;
using EconomIA.CargaDeDados.Models;

namespace EconomIA.CargaDeDados.Repositories;

public class ControlesImportacao {
	private readonly IDbConnection conexao;

	public ControlesImportacao(IDbConnection conexao) {
		this.conexao = conexao;
	}

	public async Task<ControleImportacao?> ObterAsync(long identificadorDoOrgao, string tipoDado) {
		var sql = @"
			SELECT
				identificador as Identificador,
				identificador_do_orgao as IdentificadorDoOrgao,
				tipo_dado as TipoDado,
				data_inicial_importada as DataInicialImportada,
				data_final_importada as DataFinalImportada,
				ultima_execucao as UltimaExecucao,
				registros_importados as RegistrosImportados,
				status as Status,
				mensagem_erro as MensagemErro,
				criado_em as CriadoEm,
				atualizado_em as AtualizadoEm
			FROM public.controle_de_importacao
			WHERE identificador_do_orgao = @IdentificadorDoOrgao
			AND tipo_dado = @TipoDado;
		";

		return await conexao.QueryFirstOrDefaultAsync<ControleImportacao>(sql, new {
			IdentificadorDoOrgao = identificadorDoOrgao,
			TipoDado = tipoDado
		});
	}

	public async Task<IEnumerable<ControleImportacao>> ListarPorOrgaoAsync(long identificadorDoOrgao) {
		var sql = @"
			SELECT
				identificador as Identificador,
				identificador_do_orgao as IdentificadorDoOrgao,
				tipo_dado as TipoDado,
				data_inicial_importada as DataInicialImportada,
				data_final_importada as DataFinalImportada,
				ultima_execucao as UltimaExecucao,
				registros_importados as RegistrosImportados,
				status as Status,
				mensagem_erro as MensagemErro,
				criado_em as CriadoEm,
				atualizado_em as AtualizadoEm
			FROM public.controle_de_importacao
			WHERE identificador_do_orgao = @IdentificadorDoOrgao
			ORDER BY tipo_dado;
		";

		return await conexao.QueryAsync<ControleImportacao>(sql, new { IdentificadorDoOrgao = identificadorDoOrgao });
	}

	public async Task<long> IniciarImportacaoAsync(long identificadorDoOrgao, string tipoDado) {
		var sql = @"
			INSERT INTO public.controle_de_importacao (
				identificador_do_orgao,
				tipo_dado,
				status,
				ultima_execucao
			) VALUES (
				@IdentificadorDoOrgao,
				@TipoDado,
				@Status,
				now()
			)
			ON CONFLICT (identificador_do_orgao, tipo_dado) DO UPDATE
			SET
				status = @Status,
				ultima_execucao = now(),
				mensagem_erro = NULL,
				atualizado_em = now()
			RETURNING identificador;
		";

		return await conexao.ExecuteScalarAsync<long>(sql, new {
			IdentificadorDoOrgao = identificadorDoOrgao,
			TipoDado = tipoDado,
			Status = StatusImportacao.EmAndamento
		});
	}

	public async Task FinalizarComSucessoAsync(
		long identificadorDoOrgao,
		string tipoDado,
		DateTime dataInicial,
		DateTime dataFinal,
		int registrosImportados) {
		var sql = @"
			UPDATE public.controle_de_importacao
			SET
				data_inicial_importada = LEAST(COALESCE(data_inicial_importada, @DataInicial), @DataInicial),
				data_final_importada = GREATEST(COALESCE(data_final_importada, @DataFinal), @DataFinal),
				registros_importados = @RegistrosImportados,
				status = @Status,
				mensagem_erro = NULL,
				atualizado_em = now()
			WHERE identificador_do_orgao = @IdentificadorDoOrgao
			AND tipo_dado = @TipoDado;
		";

		await conexao.ExecuteAsync(sql, new {
			IdentificadorDoOrgao = identificadorDoOrgao,
			TipoDado = tipoDado,
			DataInicial = dataInicial,
			DataFinal = dataFinal,
			RegistrosImportados = registrosImportados,
			Status = StatusImportacao.Sucesso
		});
	}

	public async Task FinalizarComErroAsync(long identificadorDoOrgao, string tipoDado, string mensagemErro) {
		var sql = @"
			UPDATE public.controle_de_importacao
			SET
				status = @Status,
				mensagem_erro = @MensagemErro,
				atualizado_em = now()
			WHERE identificador_do_orgao = @IdentificadorDoOrgao
			AND tipo_dado = @TipoDado;
		";

		await conexao.ExecuteAsync(sql, new {
			IdentificadorDoOrgao = identificadorDoOrgao,
			TipoDado = tipoDado,
			Status = StatusImportacao.Erro,
			MensagemErro = mensagemErro
		});
	}

	public async Task<DateTime?> ObterProximaDataParaImportarAsync(long identificadorDoOrgao, string tipoDado) {
		var controle = await ObterAsync(identificadorDoOrgao, tipoDado);

		if (controle is null || controle.DataFinalImportada is null) {
			return null;
		}

		return controle.DataFinalImportada.Value.AddDays(1);
	}
}
