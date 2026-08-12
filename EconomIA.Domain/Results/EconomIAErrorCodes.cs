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

	[Description("Execução de carga not found.")]
	ExecucaoCargaNotFound,

	[Description("Invalid execução de carga request.")]
	InvalidExecucaoCargaRequest,

	[Description("Invalid execução request.")]
	InvalidExecucaoRequest,

	[Description("Configuração de carga not found.")]
	ConfiguracaoCargaNotFound,

	[Description("Invalid configuração de carga request.")]
	InvalidConfiguracaoCargaRequest,

	[Description("Usuário not found.")]
	UsuarioNotFound,

	[Description("Usuário already exists.")]
	UsuarioAlreadyExists,

	[Description("Invalid usuário request.")]
	InvalidUsuarioRequest,

	[Description("Invalid credentials.")]
	InvalidCredentials,

	[Description("User is not active.")]
	UsuarioInativo,

	[Description("Ata not found.")]
	AtaNotFound,

	[Description("Invalid ata request.")]
	InvalidAtaRequest,
}

public static class EconomIAErrorCodesExtensions {
	public static String GetDescription(this EconomIAErrorCodes code) {
		var field = code.GetType().GetField(code.ToString());
		var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
		return attribute?.Description ?? $"Error: {code}";
	}
}
