using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Application.Extensions;
using EconomIA.Common.Results;
using EconomIA.Domain;
using EconomIA.Domain.Repositories;
using EconomIA.Domain.Results;

namespace EconomIA.Application.Commands.MonitorarOrgao;

public static class MonitorarOrgao {
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

			var monitoradoExistenteResult = await orgaosMonitorados.Find(OrgaosMonitoradosSpecifications.ComOrgao(orgao.Id), cancellationToken);

			if (monitoradoExistenteResult.IsSuccess) {
				var monitoradoExistente = monitoradoExistenteResult.Value;

				if (monitoradoExistente.Ativo) {
					return Success();
				}

				monitoradoExistente.Ativar();
				var updateResult = await orgaosMonitorados.Update(monitoradoExistente, cancellationToken);

				if (updateResult.IsFailure) {
					return Failure(updateResult.Error.ToOrgaoMonitoradoError());
				}

				return Success();
			}

			var novoMonitorado = OrgaoMonitorado.Criar(orgao.Id);
			var addResult = await orgaosMonitorados.Add(novoMonitorado, cancellationToken);

			if (addResult.IsFailure) {
				return Failure(addResult.Error.ToOrgaoMonitoradoError());
			}

			return Success();
		}
	}
}
