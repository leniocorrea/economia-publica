using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Results;
using EconomIA.Domain.Results;

namespace EconomIA.Application.Commands;

public abstract class CommandHandler<TCommand> : ICommandHandler<TCommand> where TCommand : ICommand {
	public abstract Task<UnitResult<HandlerResultError>> Handle(TCommand command, CancellationToken cancellationToken = default);

	protected static UnitResult<HandlerResultError> Success() => UnitResult.Success<HandlerResultError>();

	protected static UnitResult<HandlerResultError> Failure(EconomIAErrorCodes code, String message, String? hint = null) =>
		UnitResult.Failure<HandlerResultError>(new EconomIAApplicationError(code, message, hint));

	protected static UnitResult<HandlerResultError> Failure(HandlerResultError error) => UnitResult.Failure(error);
}
