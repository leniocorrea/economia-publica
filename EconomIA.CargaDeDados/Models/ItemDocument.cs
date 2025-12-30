namespace EconomIA.CargaDeDados.Models;

public class ItemDocument
{
    public long Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Orgao { get; set; } = string.Empty;
    public DateTime Data { get; set; }
}
