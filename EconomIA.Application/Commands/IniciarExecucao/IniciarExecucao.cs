using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Results;
using EconomIA.Domain;
using EconomIA.Domain.Repositories;
using static EconomIA.Domain.Results.EconomIAErrorCodes;

namespace EconomIA.Application.Commands.IniciarExecucao;

public static class IniciarExecucao {
	public record Command(
		String ModoExecucao,
		Int32? DiasRetroativos,
		String[]? Cnpjs,
		Boolean CarregarCompras = true,
		Boolean CarregarContratos = true,
		Boolean CarregarAtas = true
	) : ICommand<Response>;

	public record Response(
		Int64 Id,
		String Status,
		String ModoExecucao,
		String TipoGatilho,
		DateTime CriadoEm
	);

	public class Handler(IExecucoesCarga execucoesCarga) : CommandHandler<Command, Response> {
		private static readonly String[] ModosValidos = [ModoExecucaoTipo.Incremental, ModoExecucaoTipo.Diaria, ModoExecucaoTipo.Brasil, ModoExecucaoTipo.Reconciliacao, ModoExecucaoTipo.Enriquecimento, ModoExecucaoTipo.EnriquecimentoObjeto];

		public override async Task<Result<Response, HandlerResultError>> Handle(Command command, CancellationToken cancellationToken = default) {
			var validacao = Validar(command);

			if (validacao.IsFailure) {
				return validacao.ConvertFailure<Response>();
			}

			var parametros = new ParametrosExecucao(
				command.DiasRetroativos,
				command.Cnpjs,
				command.CarregarCompras,
				command.CarregarContratos,
				command.CarregarAtas);

			var result = await execucoesCarga.CriarPendenteAsync(
				command.ModoExecucao,
				TipoGatilhoTipo.Api,
				parametros,
				cancellationToken);

			if (result.IsFailure) {
				return Failure<Response>(InvalidExecucaoRequest, result.Error.Message);
			}

			var execucao = result.Value;

			return Success(new Response(
				execucao.Id,
				execucao.Status,
				execucao.ModoExecucao,
				execucao.TipoGatilho,
				execucao.CriadoEm));
		}

		private UnitResult<HandlerResultError> Validar(Command command) {
			if (String.IsNullOrWhiteSpace(command.ModoExecucao)) {
				return Failure(InvalidExecucaoRequest, "Modo de execução é obrigatório.");
			}

			if (!ModosValidos.Contains(command.ModoExecucao.ToLower())) {
				return Failure(InvalidExecucaoRequest, $"Modo de execução inválido. Use: {String.Join(", ", ModosValidos)}");
			}

			var modo = command.ModoExecucao.ToLower();

			if (modo == ModoExecucaoTipo.Diaria || modo == ModoExecucaoTipo.Brasil) {
				if (!command.DiasRetroativos.HasValue || command.DiasRetroativos.Value < 1) {
					return Failure(InvalidExecucaoRequest, "Dias retroativos deve ser informado e maior que zero.");
				}

				if (command.DiasRetroativos.Value > 365) {
					return Failure(InvalidExecucaoRequest, "Dias retroativos não pode ser maior que 365.");
				}
			}

			if (command.Cnpjs is not null && command.Cnpjs.Length > 0) {
				foreach (var cnpj in command.Cnpjs) {
					if (String.IsNullOrWhiteSpace(cnpj)) {
						return Failure(InvalidExecucaoRequest, "CNPJ não pode ser vazio.");
					}

					var cnpjNumeros = new String(cnpj.Where(Char.IsDigit).ToArray());

					if (cnpjNumeros.Length != 14) {
						return Failure(InvalidExecucaoRequest, $"CNPJ inválido: {cnpj}. Deve conter 14 dígitos.");
					}
				}
			}

			return UnitResult.Success<HandlerResultError>();
		}
	}
}
