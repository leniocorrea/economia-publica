using CSharpFunctionalExtensions;
using EconomIA.Common.Results;
using MediatR;

namespace EconomIA.Application.Commands;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, UnitResult<HandlerResultError>> where TCommand : ICommand;
