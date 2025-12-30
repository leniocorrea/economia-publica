namespace EconomIA.CargaDeDados.Models;

public sealed class TipoDado {
	public static readonly TipoDado Compras = new("compras");
	public static readonly TipoDado ItensCompra = new("itens_compra");
	public static readonly TipoDado ResultadosItens = new("resultados_itens");
	public static readonly TipoDado Contratos = new("contratos");
	public static readonly TipoDado Atas = new("atas");

	private static readonly TipoDado[] Todos = { Compras, ItensCompra, ResultadosItens, Contratos, Atas };

	public string Codigo { get; }

	private TipoDado(string codigo) {
		Codigo = codigo;
	}

	public static TipoDado? ObterPorCodigo(string codigo) {
		return Todos.FirstOrDefault(t => t.Codigo == codigo);
	}

	public static IEnumerable<TipoDado> ObterTodos() => Todos;

	public override string ToString() => Codigo;

	public static implicit operator string(TipoDado tipo) => tipo.Codigo;
}
