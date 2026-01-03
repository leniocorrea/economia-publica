using System;
using EconomIA.Common.Persistence;
using EconomIA.Common.Results;
using EconomIA.Domain.Results;
using static EconomIA.Common.Persistence.RepositoryErrorCode;
using static EconomIA.Domain.Results.EconomIAErrorCodes;

namespace EconomIA.Application.Extensions;

public static class RepositoryErrorExtensions {
	public static HandlerResultError ToOrgaoError(this RepositoryError error) {
		return error.Code switch {
			NotFound => new EconomIAApplicationError(OrgaoNotFound, error.Message),
			MultipleFound => new EconomIAApplicationError(MultipleOrgaosFound, error.Message),
			_ => new EconomIAApplicationError(InvalidOrgaoRequest, error.Message),
		};
	}

	public static HandlerResultError ToItemError(this RepositoryError error) {
		return error.Code switch {
			NotFound => new EconomIAApplicationError(ItemNotFound, error.Message),
			MultipleFound => new EconomIAApplicationError(MultipleItensFound, error.Message),
			_ => new EconomIAApplicationError(InvalidItemRequest, error.Message),
		};
	}

	public static HandlerResultError ToOrgaoMonitoradoError(this RepositoryError error) {
		return error.Code switch {
			NotFound => new EconomIAApplicationError(OrgaoMonitoradoNotFound, error.Message),
			_ => new EconomIAApplicationError(InvalidOrgaoMonitoradoRequest, error.Message),
		};
	}
}
