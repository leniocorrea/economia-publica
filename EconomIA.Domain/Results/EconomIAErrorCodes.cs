using System;
using System.ComponentModel;
using System.Reflection;

namespace EconomIA.Domain.Results;

public enum EconomIAErrorCodes {
	[Description("Required argument was not provided.")]
	ArgumentNotProvided,

	[Description("Invalid argument provided.")]
	InvalidArgument,

	[Description("An unexpected error occurred.")]
	OtherError,

	[Description("Órgão not found.")]
	OrgaoNotFound,

	[Description("Multiple órgãos found.")]
	MultipleOrgaosFound,

	[Description("Invalid órgão request.")]
	InvalidOrgaoRequest,

	[Description("Item not found.")]
	ItemNotFound,

	[Description("Multiple items found.")]
	MultipleItensFound,

	[Description("Invalid item request.")]
	InvalidItemRequest,

	[Description("Órgão monitorado not found.")]
	OrgaoMonitoradoNotFound,

	[Description("Órgão monitorado already exists.")]
	OrgaoMonitoradoAlreadyExists,

	[Description("Invalid órgão monitorado request.")]
	InvalidOrgaoMonitoradoRequest,
}

public static class EconomIAErrorCodesExtensions {
	public static String GetDescription(this EconomIAErrorCodes code) {
		var field = code.GetType().GetField(code.ToString());
		var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
		return attribute?.Description ?? $"Error: {code}";
	}
}
