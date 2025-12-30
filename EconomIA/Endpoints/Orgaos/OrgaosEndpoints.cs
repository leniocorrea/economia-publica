using System;
using Microsoft.AspNetCore.Routing;

namespace EconomIA.Endpoints.Orgaos;

public static class OrgaosEndpoints {
	public static IEndpointRouteBuilder MapOrgaosEndpoints(this IEndpointRouteBuilder app) {
		app.MapListOrgaos();
		app.MapGetOrgao();

		return app;
	}
}
