using System;
using EconomIA.Common.Domain;

namespace EconomIA.Domain;

public class Contrato : Aggregate {
	protected Contrato() {
		NumeroControlePncp = null!;
	}

	public Contrato(
		Int64 id,
		Int64 identificadorDoOrgao,
		String numeroControlePncp,
		Int32 anoContrato,
		Int32 sequencialContrato,
		DateTime? criadoEm = null,
		DateTime? atualizadoEm = null,
		String? numeroControlePncpCompra = null,
		String? numeroContratoEmpenho = null,
		String? processo = null,
		String? objetoContrato = null,
		Int32? tipoContratoId = null,
		String? tipoContratoNome = null,
		Int32? categoriaProcessoId = null,
		String? categoriaProcessoNome = null,
		String? niFornecedor = null,
		String? nomeRazaoSocialFornecedor = null,
		String? tipoPessoa = null,
		Decimal? valorInicial = null,
		Decimal? valorGlobal = null,
		Decimal? valorParcela = null,
		Decimal? valorAcumulado = null,
		Int32? numeroParcelas = null,
		DateTime? dataAssinatura = null,
		DateTime? dataVigenciaInicio = null,
		DateTime? dataVigenciaFim = null,
		DateTime? dataPublicacaoPncp = null,
		DateTime? dataAtualizacao = null,
		DateTime? dataAtualizacaoGlobal = null,
		Boolean receita = false,
		String? informacaoComplementar = null,
		String? usuarioNome = null) : base(id) {
		IdentificadorDoOrgao = identificadorDoOrgao;
		NumeroControlePncp = numeroControlePncp;
		AnoContrato = anoContrato;
		SequencialContrato = sequencialContrato;
		CriadoEm = criadoEm;
		AtualizadoEm = atualizadoEm;
		NumeroControlePncpCompra = numeroControlePncpCompra;
		NumeroContratoEmpenho = numeroContratoEmpenho;
		Processo = processo;
		ObjetoContrato = objetoContrato;
		TipoContratoId = tipoContratoId;
		TipoContratoNome = tipoContratoNome;
		CategoriaProcessoId = categoriaProcessoId;
		CategoriaProcessoNome = categoriaProcessoNome;
		NiFornecedor = niFornecedor;
		NomeRazaoSocialFornecedor = nomeRazaoSocialFornecedor;
		TipoPessoa = tipoPessoa;
		ValorInicial = valorInicial;
		ValorGlobal = valorGlobal;
		ValorParcela = valorParcela;
		ValorAcumulado = valorAcumulado;
		NumeroParcelas = numeroParcelas;
		DataAssinatura = dataAssinatura;
		DataVigenciaInicio = dataVigenciaInicio;
		DataVigenciaFim = dataVigenciaFim;
		DataPublicacaoPncp = dataPublicacaoPncp;
		DataAtualizacao = dataAtualizacao;
		DataAtualizacaoGlobal = dataAtualizacaoGlobal;
		Receita = receita;
		InformacaoComplementar = informacaoComplementar;
		UsuarioNome = usuarioNome;
	}

	public virtual Int64 IdentificadorDoOrgao { get; protected set; }
	public virtual String NumeroControlePncp { get; protected set; }
	public virtual String? NumeroControlePncpCompra { get; protected set; }
	public virtual Int32 AnoContrato { get; protected set; }
	public virtual Int32 SequencialContrato { get; protected set; }
	public virtual String? NumeroContratoEmpenho { get; protected set; }
	public virtual String? Processo { get; protected set; }
	public virtual String? ObjetoContrato { get; protected set; }
	public virtual Int32? TipoContratoId { get; protected set; }
	public virtual String? TipoContratoNome { get; protected set; }
	public virtual Int32? CategoriaProcessoId { get; protected set; }
	public virtual String? CategoriaProcessoNome { get; protected set; }
	public virtual String? NiFornecedor { get; protected set; }
	public virtual String? NomeRazaoSocialFornecedor { get; protected set; }
	public virtual String? TipoPessoa { get; protected set; }
	public virtual Decimal? ValorInicial { get; protected set; }
	public virtual Decimal? ValorGlobal { get; protected set; }
	public virtual Decimal? ValorParcela { get; protected set; }
	public virtual Decimal? ValorAcumulado { get; protected set; }
	public virtual Int32? NumeroParcelas { get; protected set; }
	public virtual DateTime? DataAssinatura { get; protected set; }
	public virtual DateTime? DataVigenciaInicio { get; protected set; }
	public virtual DateTime? DataVigenciaFim { get; protected set; }
	public virtual DateTime? DataPublicacaoPncp { get; protected set; }
	public virtual DateTime? DataAtualizacao { get; protected set; }
	public virtual DateTime? DataAtualizacaoGlobal { get; protected set; }
	public virtual Boolean Receita { get; protected set; }
	public virtual String? InformacaoComplementar { get; protected set; }
	public virtual String? UsuarioNome { get; protected set; }
	public virtual DateTime? CriadoEm { get; protected set; }
	public virtual DateTime? AtualizadoEm { get; protected set; }

	public virtual Orgao? Orgao { get; protected set; }
}
