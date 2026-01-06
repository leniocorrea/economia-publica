using System;
using System.Collections.Generic;
using System.Net;
using CSharpFunctionalExtensions;
using EconomIA.Common.Results;
using EconomIA.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static EconomIA.Domain.Results.EconomIAErrorCodes;
using HttpResult = Microsoft.AspNetCore.Http.IResult;

namespace EconomIA.Results;

public static class ResultExtensions {
	private static readonly Dictionary<EconomIAErrorCodes, Func<HandlerResultError, HttpResult>> ErrorCodeMappings = new() {
		[OrgaoNotFound] = NotFound,
		[OrgaoMonitoradoNotFound] = NotFound,
		[ConfiguracaoCargaNotFound] = NotFound,

		[MultipleOrgaosFound] = Conflict,
		[OrgaoMonitoradoAlreadyExists] = Conflict,

		[ArgumentNotProvided] = BadRequest,
		[InvalidArgument] = BadRequest,
		[InvalidOrgaoRequest] = BadRequest,
		[InvalidOrgaoMonitoradoRequest] = BadRequest,
		[InvalidConfiguracaoCargaRequest] = BadRequest,
		[OtherError] = BadRequest,
	};

	public static HttpResult ToOk<TResponse, TResult>(this Result<TResponse, HandlerResultError> result, Func<TResponse, TResult> mapper) {
		return result.IsSuccess ? TypedResults.Ok(mapper(result.Value)) : MapErrorToResponse(result.Error);
	}

	public static HttpResult ToCreated(this UnitResult<HandlerResultError> result, String location) {
		return result.IsSuccess ? TypedResults.Created(location) : MapErrorToResponse(result.Error);
	}

	public static HttpResult ToNoContent(this UnitResult<HandlerResultError> result) {
		return result.IsSuccess ? TypedResults.NoContent() : MapErrorToResponse(result.Error);
	}

	public static HttpResult ToErrorResponse<TResponse>(this Result<TResponse, HandlerResultError> result) {
		return MapErrorToResponse(result.Error);
	}

	private static HttpResult MapErrorToResponse(HandlerResultError error) {
		var code = (EconomIAErrorCodes)error.Code.Value;

		if (ErrorCodeMappings.TryGetValue(code, out var mapper)) {
			return mapper(error);
		}

		return BadRequest(error);
	}

	private static Dictionary<String, Object?> CreateExtensions(HandlerResultError error) {
		var code = error.Code
			.Map(code => (EconomIAErrorCodes)code)
			.GetValueOrThrow();

		var extensions = new Dictionary<String, Object?> {
			["code"] = code,
		};

		if (error is EconomIAApplicationError { Hint: not null } economiaError) {
			extensions["hint"] = economiaError.Hint;
		}

		return extensions;
	}

	private static HttpResult NotFound(HandlerResultError error) {
		return TypedResults.NotFound(new ProblemDetails {
			Status = (Int32)HttpStatusCode.NotFound,
			Detail = error.ResultError.ToProblemString(),
			Extensions = CreateExtensions(error),
		});
	}

	private static HttpResult Conflict(HandlerResultError error) {
		return TypedResults.Conflict(new ProblemDetails {
			Status = (Int32)HttpStatusCode.Conflict,
			Detail = error.ResultError.ToProblemString(),
			Extensions = CreateExtensions(error),
		});
	}

	private static HttpResult BadRequest(HandlerResultError error) {
		return TypedResults.BadRequest(new ProblemDetails {
			Status = (Int32)HttpStatusCode.BadRequest,
			Detail = error.ResultError.ToProblemString(),
			Extensions = CreateExtensions(error),
		});
	}
}
