using System.Text.Json.Serialization;

namespace EconomIA.CargaDeDados.Models;

public class ItemDocument {
	public Int64 Id { get; set; }
	public String Descricao { get; set; } = String.Empty;
	public Decimal Valor { get; set; }
	public String Orgao { get; set; } = String.Empty;
	public DateTime Data { get; set; }
	public DateTime? DataInclusao { get; set; }
	public String? UfSigla { get; set; }
	public Decimal? ValorUnitarioHomologado { get; set; }
	public Decimal? ValorTotalHomologado { get; set; }
}

public record AtasDoItem(
	Int64 Id,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] DateTime? AtaVigenciaFim,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] DateTime? AtaDataDeReferencia,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] DateTime? AtaAdesaoVigenciaFim);
