using System;
using Microsoft.AspNetCore.Routing;

namespace EconomIA.Endpoints.ItensDaCompra;

public static class ItensDaCompraEndpoints {
	public static IEndpointRouteBuilder MapItensDaCompraEndpoints(this IEndpointRouteBuilder app) {
		app.MapSearchItensDaCompra();

		return app;
	}
}
