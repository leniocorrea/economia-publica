using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Results;
using EconomIA.Domain.Repositories;
using static EconomIA.Domain.Results.EconomIAErrorCodes;

namespace EconomIA.Application.Commands.AtualizarConfiguracao;

public static class AtualizarConfiguracao {
	public record Command(
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
		Int32 DiaSemanasSincronizacao) : ICommand;

	public class Handler(IConfiguracoesCarga configuracoesCarga) : CommandHandler<Command> {
		public override async Task<UnitResult<HandlerResultError>> Handle(Command command, CancellationToken cancellationToken = default) {
			var validationResult = Validar(command);

			if (validationResult.IsFailure) {
				return validationResult;
			}

			var configResult = await configuracoesCarga.ObterOuCriarPadrao(cancellationToken);

			if (configResult.IsFailure) {
				return Failure(ConfiguracaoCargaNotFound, configResult.Error.Message);
			}

			var config = configResult.Value;

			if (!TimeOnly.TryParse(command.HorarioExecucao, out var horarioExecucao)) {
				return Failure(InvalidConfiguracaoCargaRequest, "Horário de execução inválido.");
			}

			if (!TimeOnly.TryParse(command.HorarioSincronizacao, out var horarioSincronizacao)) {
				return Failure(InvalidConfiguracaoCargaRequest, "Horário de sincronização inválido.");
			}

			config.Atualizar(
				horarioExecucao: horarioExecucao,
				diasSemana: command.DiasSemana,
				habilitado: command.Habilitado,
				diasRetroativos: command.DiasRetroativos,
				diasCargaInicial: command.DiasCargaInicial,
				maxConcorrencia: command.MaxConcorrencia,
				carregarCompras: command.CarregarCompras,
				carregarContratos: command.CarregarContratos,
				carregarAtas: command.CarregarAtas,
				sincronizarOrgaos: command.SincronizarOrgaos,
				horarioSincronizacao: horarioSincronizacao,
				diaSemanasSincronizacao: command.DiaSemanasSincronizacao
			);

			var updateResult = await configuracoesCarga.Update(config, cancellationToken);

			if (updateResult.IsFailure) {
				return Failure(InvalidConfiguracaoCargaRequest, updateResult.Error.Message);
			}

			return Success();
		}

		private UnitResult<HandlerResultError> Validar(Command command) {
			if (String.IsNullOrWhiteSpace(command.HorarioExecucao)) {
				return Failure(InvalidConfiguracaoCargaRequest, "Horário de execução é obrigatório.");
			}

			if (!TimeOnly.TryParse(command.HorarioExecucao, out _)) {
				return Failure(InvalidConfiguracaoCargaRequest, "Horário de execução inválido. Use formato HH:mm.");
			}

			if (String.IsNullOrWhiteSpace(command.HorarioSincronizacao)) {
				return Failure(InvalidConfiguracaoCargaRequest, "Horário de sincronização é obrigatório.");
			}

			if (!TimeOnly.TryParse(command.HorarioSincronizacao, out _)) {
				return Failure(InvalidConfiguracaoCargaRequest, "Horário de sincronização inválido. Use formato HH:mm.");
			}

			if (command.DiasSemana is null || command.DiasSemana.Length == 0) {
				return Failure(InvalidConfiguracaoCargaRequest, "Pelo menos um dia da semana deve ser selecionado.");
			}

			if (command.DiasSemana.Any(d => d < 0 || d > 6)) {
				return Failure(InvalidConfiguracaoCargaRequest, "Dias da semana devem estar entre 0 (Domingo) e 6 (Sábado).");
			}

			if (command.DiaSemanasSincronizacao < 0 || command.DiaSemanasSincronizacao > 6) {
				return Failure(InvalidConfiguracaoCargaRequest, "Dia de sincronização deve estar entre 0 (Domingo) e 6 (Sábado).");
			}

			if (command.DiasRetroativos < 1 || command.DiasRetroativos > 365) {
				return Failure(InvalidConfiguracaoCargaRequest, "Dias retroativos deve estar entre 1 e 365.");
			}

			if (command.DiasCargaInicial < 1 || command.DiasCargaInicial > 365) {
				return Failure(InvalidConfiguracaoCargaRequest, "Dias de carga inicial deve estar entre 1 e 365.");
			}

			if (command.MaxConcorrencia < 1 || command.MaxConcorrencia > 20) {
				return Failure(InvalidConfiguracaoCargaRequest, "Concorrência máxima deve estar entre 1 e 20.");
			}

			return Success();
		}
	}
}
