using System;
using CSharpFunctionalExtensions;
using EconomIA.Common.Results;
using MediatR;

namespace EconomIA.Application.Queries;

public interface IQuery<TResponse> : IRequest<Result<TResponse, HandlerResultError>>;
