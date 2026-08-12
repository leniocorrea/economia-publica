using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace EconomIA.Adapters.Persistence.Repositories.Atas;

public record CursorDaAta(DateTime DataDeReferencia, Int64 Id) {
	public static String Escrever(DateTime dataDeReferencia, Int64 id) {
		return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{dataDeReferencia.Ticks}|{id}"));
	}

	public static Boolean TentarLer(String? cursor, [NotNullWhen(true)] out CursorDaAta? valor) {
		valor = null;

		if (String.IsNullOrWhiteSpace(cursor)) {
			return false;
		}

		try {
			var conteudo = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
			var partes = conteudo.Split('|');

			if (partes.Length != 2 || !Int64.TryParse(partes[0], out var ticks) || !Int64.TryParse(partes[1], out var id)) {
				return false;
			}

			if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks) {
				return false;
			}

			valor = new CursorDaAta(new DateTime(ticks, DateTimeKind.Utc), id);
			return true;
		} catch (FormatException) {
			return false;
		}
	}
}
