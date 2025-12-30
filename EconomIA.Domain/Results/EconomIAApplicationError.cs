using System;
using System.Collections.Generic;
using EconomIA.Common.Results;

namespace EconomIA.Domain.Results;

public sealed class EconomIAApplicationError : HandlerResultError {
	public EconomIAApplicationError(EconomIAErrorCodes code, String message) : this(code, message, null) { }

	public EconomIAApplicationError(EconomIAErrorCodes code, String message, String? hint) : base((Int32)code, CreateDictionary(code, message)) {
		Hint = hint;
	}

	public String? Hint { get; }

	private static Dictionary<String, String[]> CreateDictionary(EconomIAErrorCodes code, String message) {
		return new(StringComparer.CurrentCultureIgnoreCase) {
			[Enum.GetName(code) ?? "unknown"] = [message],
		};
	}
}
