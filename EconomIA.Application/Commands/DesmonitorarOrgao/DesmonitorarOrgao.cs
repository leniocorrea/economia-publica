using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Application.Extensions;
using EconomIA.Common.Results;
using EconomIA.Domain.Repositories;
using EconomIA.Domain.Results;

namespace EconomIA.Application.Commands.DesmonitorarOrgao;

public static class DesmonitorarOrgao {
	public record Command(String Cnpj) : ICommand;

	public class Handler(IOrgaosReader orgaosReader, IOrgaosMonitorados orgaosMonitorados) : CommandHandler<Command> {
		public override async Task<UnitResult<HandlerResultError>> Handle(Command command, CancellationToken cancellationToken = default) {
			if (String.IsNullOrWhiteSpace(command.Cnpj)) {
				return Failure(EconomIAErrorCodes.ArgumentNotProvided, "CNPJ é obrigatório.");
			}

			var orgaoResult = await orgaosReader.Find(OrgaosSpecifications.WithCnpj(command.Cnpj), cancellationToken);

			if (orgaoResult.IsFailure) {
				return Failure(EconomIAErrorCodes.OrgaoNotFound, $"Órgão com CNPJ '{command.Cnpj}' não encontrado.");
			}

			var orgao = orgaoResult.Value;

			var monitoradoResult = await orgaosMonitorados.Find(OrgaosMonitoradosSpecifications.ComOrgao(orgao.Id), cancellationToken);

			if (monitoradoResult.IsFailure) {
				return Failure(EconomIAErrorCodes.OrgaoMonitoradoNotFound, $"Órgão com CNPJ '{command.Cnpj}' não está sendo monitorado.");
			}

			var monitorado = monitoradoResult.Value;

			if (!monitorado.Ativo) {
				return Success();
			}

			monitorado.Desativar();
			var updateResult = await orgaosMonitorados.Update(monitorado, cancellationToken);

			if (updateResult.IsFailure) {
				return Failure(updateResult.Error.ToOrgaoMonitoradoError());
			}

			return Success();
		}
	}
}
