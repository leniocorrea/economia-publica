using System;
using EconomIA.Adapters.Persistence.Repositories.Atas;
using FluentAssertions;
using Xunit;

namespace EconomIA.Adapters.Tests.Persistence.Repositories.Atas;

public class CursorDaAtaTests {
	[Fact]
	public void escrever_e_ler_preserva_data_e_identificador() {
		var data = new DateTime(2026, 8, 6, 14, 32, 17);

		var cursor = CursorDaAta.Escrever(data, 4321);

		CursorDaAta.TentarLer(cursor, out var lido).Should().BeTrue();
		lido!.DataDeReferencia.Should().Be(data);
		lido.Id.Should().Be(4321);
	}

	[Fact]
	public void data_lida_do_cursor_e_utc() {
		var cursor = CursorDaAta.Escrever(new DateTime(2026, 8, 6, 14, 32, 17, DateTimeKind.Utc), 1);

		CursorDaAta.TentarLer(cursor, out var lido).Should().BeTrue();
		lido!.DataDeReferencia.Kind.Should().Be(DateTimeKind.Utc);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("nao-e-base64!")]
	[InlineData("c2VtLXNlcGFyYWRvcg==")]
	public void cursor_invalido_nao_e_lido(String? cursor) {
		CursorDaAta.TentarLer(cursor, out var lido).Should().BeFalse();
		lido.Should().BeNull();
	}
}
