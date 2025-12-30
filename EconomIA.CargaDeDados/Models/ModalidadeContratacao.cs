namespace EconomIA.CargaDeDados.Models;

public class ModalidadeContratacao : IEquatable<ModalidadeContratacao>, IComparable<ModalidadeContratacao> {
	public static ModalidadeContratacao Concorrencia { get; } = new(1, "Concorrência");
	public static ModalidadeContratacao Concurso { get; } = new(2, "Concurso");
	public static ModalidadeContratacao Leilao { get; } = new(3, "Leilão");
	public static ModalidadeContratacao Pregao { get; } = new(4, "Pregão");
	public static ModalidadeContratacao DialogoCompetitivo { get; } = new(5, "Diálogo Competitivo");
	public static ModalidadeContratacao DispensaDeLicitacao { get; } = new(6, "Dispensa de Licitação");
	public static ModalidadeContratacao InexigibilidadeDeLicitacao { get; } = new(7, "Inexigibilidade de Licitação");

	public static IEnumerable<ModalidadeContratacao> Todos() {
		yield return Concorrencia;
		yield return Concurso;
		yield return Leilao;
		yield return Pregao;
		yield return DialogoCompetitivo;
		yield return DispensaDeLicitacao;
		yield return InexigibilidadeDeLicitacao;
	}

	private ModalidadeContratacao(int codigo, string nome) {
		Codigo = codigo;
		Nome = nome;
	}

	public int Codigo { get; }
	public string Nome { get; }

	public override string ToString() => Nome;

	public override bool Equals(object? outroObjeto) => Equals(outroObjeto as ModalidadeContratacao);

	public bool Equals(ModalidadeContratacao? outro) => ReferenceEquals(this, outro);

	public override int GetHashCode() => Nome.GetHashCode();

	public static bool operator ==(ModalidadeContratacao? x, ModalidadeContratacao? y) {
		if (ReferenceEquals(x, null)) {
			return ReferenceEquals(y, null);
		}

		return x.Equals(y);
	}

	public static bool operator !=(ModalidadeContratacao? x, ModalidadeContratacao? y) => !(x == y);

	public int CompareTo(ModalidadeContratacao? outro) {
		if (ReferenceEquals(outro, null)) {
			return int.MaxValue;
		}

		return string.Compare(ToString(), outro.ToString(), StringComparison.Ordinal);
	}
}

public class ErroDeNegocio : Exception {
	public ErroDeNegocio(string mensagem, Exception? erroOriginal = null) : base(mensagem, erroOriginal) {
	}
}
