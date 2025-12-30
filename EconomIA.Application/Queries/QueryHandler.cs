using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Results;
using EconomIA.Domain.Results;

namespace EconomIA.Application.Queries;

public abstract class QueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse> {
	public abstract Task<Result<TResponse, HandlerResultError>> Handle(TQuery query, CancellationToken cancellationToken = default);

	protected static Result<TResponse, HandlerResultError> Success(TResponse value) {
		return Result.Success<TResponse, HandlerResultError>(value);
	}

	protected static Result<TResponse, HandlerResultError> Failure(EconomIAErrorCodes code, String message) {
		return Result.Failure<TResponse, HandlerResultError>(new EconomIAApplicationError(code, message));
	}

	protected static Result<TResponse, HandlerResultError> Failure(HandlerResultError error) {
		return Result.Failure<TResponse, HandlerResultError>(error);
	}
}
