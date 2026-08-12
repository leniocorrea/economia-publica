using System;
using Microsoft.AspNetCore.Routing;

namespace EconomIA.Endpoints.Atas;

public static class AtasEndpoints {
	public static IEndpointRouteBuilder MapAtasEndpoints(this IEndpointRouteBuilder app) {
		app.MapListAtas();

		return app;
	}
}
