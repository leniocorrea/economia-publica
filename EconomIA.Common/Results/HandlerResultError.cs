using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;

namespace EconomIA.Common.Results;

public abstract class HandlerResultError {
	protected HandlerResultError(Int32 code, IDictionary<String, String[]> message) {
		Code = Maybe<Int32>.From(code);
		ResultError = message;
	}

	protected HandlerResultError(IDictionary<String, String[]> message) {
		Code = Maybe<Int32>.None;
		ResultError = message;
	}

	public Maybe<Int32> Code { get; }

	public IDictionary<String, String[]> ResultError { get; }
}

public static class HandlerResultErrorExtensions {
	public static String ToProblemString(this IDictionary<String, String[]> resultError) {
		var messages = resultError.Values.SelectMany(v => v);
		return String.Join("; ", messages);
	}
}
