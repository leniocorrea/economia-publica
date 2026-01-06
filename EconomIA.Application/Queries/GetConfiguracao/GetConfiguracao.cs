using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Results;
using EconomIA.Domain.Repositories;
using static EconomIA.Domain.Results.EconomIAErrorCodes;

namespace EconomIA.Application.Queries.GetConfiguracao;

public static class GetConfiguracao {
	public record Query : IQuery<Response>;

	public record Response(
		String HorarioExecucao,
		Int32[] DiasSemana,
		Boolean Habilitado,
		Int32 DiasRetroativos,
		Int32 DiasCargaInicial,
		Int32 MaxConcorrencia,
		Boolean CarregarCompras,
		Boolean CarregarContratos,
		Boolean CarregarAtas,
		Boolean SincronizarOrgaos,
		String HorarioSincronizacao,
		Int32 DiaSemanasSincronizacao,
		DateTime AtualizadoEm);

	public class Handler(IConfiguracoesCarga configuracoesCarga) : QueryHandler<Query, Response> {
		public override async Task<Result<Response, HandlerResultError>> Handle(Query query, CancellationToken cancellationToken = default) {
			var result = await configuracoesCarga.ObterOuCriarPadrao(cancellationToken);

			if (result.IsFailure) {
				return Failure(ConfiguracaoCargaNotFound, result.Error.Message);
			}

			var config = result.Value;

			var response = new Response(
				HorarioExecucao: config.HorarioExecucao.ToString("HH:mm"),
				DiasSemana: config.DiasSemana,
				Habilitado: config.Habilitado,
				DiasRetroativos: config.DiasRetroativos,
				DiasCargaInicial: config.DiasCargaInicial,
				MaxConcorrencia: config.MaxConcorrencia,
				CarregarCompras: config.CarregarCompras,
				CarregarContratos: config.CarregarContratos,
				CarregarAtas: config.CarregarAtas,
				SincronizarOrgaos: config.SincronizarOrgaos,
				HorarioSincronizacao: config.HorarioSincronizacao.ToString("HH:mm"),
				DiaSemanasSincronizacao: config.DiaSemanasSincronizacao,
				AtualizadoEm: config.AtualizadoEm
			);

			return Success(response);
		}
	}
}
