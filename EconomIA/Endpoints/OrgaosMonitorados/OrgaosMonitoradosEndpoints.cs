using System;
using Microsoft.AspNetCore.Routing;

namespace EconomIA.Endpoints.OrgaosMonitorados;

public static class OrgaosMonitoradosEndpoints {
	public static IEndpointRouteBuilder MapOrgaosMonitoradosEndpoints(this IEndpointRouteBuilder app) {
		app.MapListOrgaosMonitorados();
		app.MapMonitorarOrgao();
		app.MapDesmonitorarOrgao();

		return app;
	}
}
