using System.Text.Json.Serialization;

namespace EconomIA.CargaDeDados.Dtos;

public record PncpResponse(
	[property: JsonPropertyName("data")] List<PncpCompraDto> Data,
	[property: JsonPropertyName("totalRegistros")] long TotalRegistros,
	[property: JsonPropertyName("totalPaginas")] int TotalPaginas,
	[property: JsonPropertyName("numeroPagina")] int NumeroPagina,
	[property: JsonPropertyName("paginasRestantes")] int PaginasRestantes,
	[property: JsonPropertyName("empty")] bool Empty
);

public record PncpCompraDto(
	[property: JsonPropertyName("amparoLegal")] PncpAmparoLegalDto? AmparoLegal,
	[property: JsonPropertyName("dataAberturaProposta")] DateTime? DataAberturaProposta,
	[property: JsonPropertyName("dataEncerramentoProposta")] DateTime? DataEncerramentoProposta,
	[property: JsonPropertyName("informacaoComplementar")] string? InformacaoComplementar,
	[property: JsonPropertyName("processo")] string? Processo,
	[property: JsonPropertyName("objetoCompra")] string? ObjetoCompra,
	[property: JsonPropertyName("linkSistemaOrigem")] string? LinkSistemaOrigem,
	[property: JsonPropertyName("justificativaPresencial")] string? JustificativaPresencial,
	[property: JsonPropertyName("unidadeSubRogada")] PncpUnidadeDto? UnidadeSubRogada,
	[property: JsonPropertyName("orgaoSubRogado")] PncpOrgaoDto? OrgaoSubRogado,
	[property: JsonPropertyName("valorTotalHomologado")] decimal? ValorTotalHomologado,
	[property: JsonPropertyName("srp")] bool Srp,
	[property: JsonPropertyName("orgaoEntidade")] PncpOrgaoDto OrgaoEntidade,
	[property: JsonPropertyName("anoCompra")] int AnoCompra,
	[property: JsonPropertyName("sequencialCompra")] int SequencialCompra,
	[property: JsonPropertyName("dataInclusao")] DateTime DataInclusao,
	[property: JsonPropertyName("dataPublicacaoPncp")] DateTime DataPublicacaoPncp,
	[property: JsonPropertyName("dataAtualizacao")] DateTime DataAtualizacao,
	[property: JsonPropertyName("numeroCompra")] string? NumeroCompra,
	[property: JsonPropertyName("unidadeOrgao")] PncpUnidadeDto? UnidadeOrgao,
	[property: JsonPropertyName("modoDisputaId")] int? ModoDisputaId,
	[property: JsonPropertyName("modalidadeId")] int ModalidadeId,
	[property: JsonPropertyName("numeroControlePNCP")] string NumeroControlePncp,
	[property: JsonPropertyName("dataAtualizacaoGlobal")] DateTime? DataAtualizacaoGlobal,
	[property: JsonPropertyName("linkProcessoEletronico")] string? LinkProcessoEletronico,
	[property: JsonPropertyName("valorTotalEstimado")] decimal? ValorTotalEstimado,
	[property: JsonPropertyName("modalidadeNome")] string? ModalidadeNome,
	[property: JsonPropertyName("modoDisputaNome")] string? ModoDisputaNome,
	[property: JsonPropertyName("tipoInstrumentoConvocatorioCodigo")] int? TipoInstrumentoConvocatorioCodigo,
	[property: JsonPropertyName("tipoInstrumentoConvocatorioNome")] string? TipoInstrumentoConvocatorioNome,
	[property: JsonPropertyName("situacaoCompraId")] int? SituacaoCompraId,
	[property: JsonPropertyName("situacaoCompraNome")] string? SituacaoCompraNome,
	[property: JsonPropertyName("usuarioNome")] string? UsuarioNome
);

public record PncpAmparoLegalDto(
	[property: JsonPropertyName("descricao")] string? Descricao,
	[property: JsonPropertyName("codigo")] long Codigo,
	[property: JsonPropertyName("nome")] string? Nome
);

public record PncpUnidadeDto(
	[property: JsonPropertyName("ufNome")] string? UfNome,
	[property: JsonPropertyName("ufSigla")] string? UfSigla,
	[property: JsonPropertyName("municipioNome")] string? MunicipioNome,
	[property: JsonPropertyName("codigoUnidade")] string? CodigoUnidade,
	[property: JsonPropertyName("nomeUnidade")] string? NomeUnidade,
	[property: JsonPropertyName("codigoIbge")] string? CodigoIbge
);

public record PncpOrgaoDto(
	[property: JsonPropertyName("cnpj")] string Cnpj,
	[property: JsonPropertyName("razaoSocial")] string RazaoSocial,
	[property: JsonPropertyName("poderId")] string? PoderId,
	[property: JsonPropertyName("esferaId")] string? EsferaId
);

public record PncpItemDto(
	[property: JsonPropertyName("numeroItem")] int NumeroItem,
	[property: JsonPropertyName("descricao")] string? Descricao,
	[property: JsonPropertyName("quantidade")] decimal? Quantidade,
	[property: JsonPropertyName("unidadeMedida")] string? UnidadeMedida,
	[property: JsonPropertyName("valorUnitarioEstimado")] decimal? ValorUnitarioEstimado,
	[property: JsonPropertyName("valorTotal")] decimal? ValorTotal,
	[property: JsonPropertyName("criterioJulgamentoNome")] string? CriterioJulgamentoNome,
	[property: JsonPropertyName("situacaoCompraItemNome")] string? SituacaoCompraItemNome,
	[property: JsonPropertyName("temResultado")] bool TemResultado
);

public record PncpResultadoDto(
	[property: JsonPropertyName("sequencialResultado")] int SequencialResultado,
	[property: JsonPropertyName("niFornecedor")] string? NiFornecedor,
	[property: JsonPropertyName("tipoPessoa")] string? TipoPessoa,
	[property: JsonPropertyName("nomeRazaoSocialFornecedor")] string? NomeRazaoSocialFornecedor,
	[property: JsonPropertyName("porteFornecedorNome")] string? PorteFornecedorNome,
	[property: JsonPropertyName("codigoPais")] string? CodigoPais,
	[property: JsonPropertyName("valorTotalHomologado")] decimal? ValorTotalHomologado,
	[property: JsonPropertyName("valorUnitarioHomologado")] decimal? ValorUnitarioHomologado,
	[property: JsonPropertyName("quantidadeHomologada")] decimal? QuantidadeHomologada,
	[property: JsonPropertyName("percentualDesconto")] decimal? PercentualDesconto,
	[property: JsonPropertyName("situacaoCompraItemResultadoNome")] string? SituacaoCompraItemResultadoNome,
	[property: JsonPropertyName("dataResultado")] DateTime? DataResultado,
	[property: JsonPropertyName("indicadorSubcontratacao")] bool IndicadorSubcontratacao
);

public record PncpOrgaoConsultaDto(
	[property: JsonPropertyName("cnpj")] string Cnpj,
	[property: JsonPropertyName("razaoSocial")] string RazaoSocial,
	[property: JsonPropertyName("nomeFantasia")] string? NomeFantasia,
	[property: JsonPropertyName("codigoNaturezaJuridica")] string? CodigoNaturezaJuridica,
	[property: JsonPropertyName("descricaoNaturezaJuridica")] string? DescricaoNaturezaJuridica,
	[property: JsonPropertyName("poderId")] string? PoderId,
	[property: JsonPropertyName("esferaId")] string? EsferaId,
	[property: JsonPropertyName("situacaoCadastral")] string? SituacaoCadastral,
	[property: JsonPropertyName("motivoSituacaoCadastral")] string? MotivoSituacaoCadastral,
	[property: JsonPropertyName("dataSituacaoCadastral")] DateTime? DataSituacaoCadastral,
	[property: JsonPropertyName("dataValidacao")] DateTime? DataValidacao,
	[property: JsonPropertyName("validado")] bool Validado,
	[property: JsonPropertyName("dataInclusao")] DateTime? DataInclusao,
	[property: JsonPropertyName("dataAtualizacao")] DateTime? DataAtualizacao,
	[property: JsonPropertyName("statusAtivo")] bool StatusAtivo,
	[property: JsonPropertyName("justificativaAtualizacao")] string? JustificativaAtualizacao
);

public record PncpUnidadeConsultaDto(
	[property: JsonPropertyName("id")] long Id,
	[property: JsonPropertyName("codigoUnidade")] string CodigoUnidade,
	[property: JsonPropertyName("nomeUnidade")] string NomeUnidade,
	[property: JsonPropertyName("municipio")] PncpMunicipioDto? Municipio,
	[property: JsonPropertyName("dataInclusao")] DateTime? DataInclusao,
	[property: JsonPropertyName("dataAtualizacao")] DateTime? DataAtualizacao,
	[property: JsonPropertyName("statusAtivo")] bool StatusAtivo,
	[property: JsonPropertyName("justificativaAtualizacao")] string? JustificativaAtualizacao
);

public record PncpMunicipioDto(
	[property: JsonPropertyName("id")] long Id,
	[property: JsonPropertyName("nome")] string? Nome,
	[property: JsonPropertyName("codigoIbge")] string? CodigoIbge,
	[property: JsonPropertyName("uf")] PncpUfDto? Uf
);

public record PncpUfDto(
	[property: JsonPropertyName("siglaUF")] string? SiglaUf,
	[property: JsonPropertyName("nomeUF")] string? NomeUf
);

public record PncpContratosResponse(
	[property: JsonPropertyName("data")] List<PncpContratoDto> Data,
	[property: JsonPropertyName("totalRegistros")] long TotalRegistros,
	[property: JsonPropertyName("totalPaginas")] int TotalPaginas,
	[property: JsonPropertyName("numeroPagina")] int NumeroPagina,
	[property: JsonPropertyName("paginasRestantes")] int PaginasRestantes,
	[property: JsonPropertyName("empty")] bool Empty
);

public record PncpContratoDto(
	[property: JsonPropertyName("numeroControlePNCP")] string NumeroControlePncp,
	[property: JsonPropertyName("numeroControlePncpCompra")] string? NumeroControlePncpCompra,
	[property: JsonPropertyName("anoContrato")] int AnoContrato,
	[property: JsonPropertyName("sequencialContrato")] int SequencialContrato,
	[property: JsonPropertyName("numeroContratoEmpenho")] string? NumeroContratoEmpenho,
	[property: JsonPropertyName("processo")] string? Processo,
	[property: JsonPropertyName("objetoContrato")] string? ObjetoContrato,
	[property: JsonPropertyName("tipoContrato")] PncpTipoContratoDto? TipoContrato,
	[property: JsonPropertyName("categoriaProcesso")] PncpCategoriaProcessoDto? CategoriaProcesso,
	[property: JsonPropertyName("orgaoEntidade")] PncpOrgaoDto OrgaoEntidade,
	[property: JsonPropertyName("niFornecedor")] string? NiFornecedor,
	[property: JsonPropertyName("nomeRazaoSocialFornecedor")] string? NomeRazaoSocialFornecedor,
	[property: JsonPropertyName("tipoPessoa")] string? TipoPessoa,
	[property: JsonPropertyName("valorInicial")] decimal? ValorInicial,
	[property: JsonPropertyName("valorGlobal")] decimal? ValorGlobal,
	[property: JsonPropertyName("valorParcela")] decimal? ValorParcela,
	[property: JsonPropertyName("valorAcumulado")] decimal? ValorAcumulado,
	[property: JsonPropertyName("numeroParcelas")] int? NumeroParcelas,
	[property: JsonPropertyName("dataAssinatura")] DateTime? DataAssinatura,
	[property: JsonPropertyName("dataVigenciaInicio")] DateTime? DataVigenciaInicio,
	[property: JsonPropertyName("dataVigenciaFim")] DateTime? DataVigenciaFim,
	[property: JsonPropertyName("dataPublicacaoPncp")] DateTime? DataPublicacaoPncp,
	[property: JsonPropertyName("dataAtualizacao")] DateTime? DataAtualizacao,
	[property: JsonPropertyName("dataAtualizacaoGlobal")] DateTime? DataAtualizacaoGlobal,
	[property: JsonPropertyName("receita")] bool Receita,
	[property: JsonPropertyName("informacaoComplementar")] string? InformacaoComplementar,
	[property: JsonPropertyName("usuarioNome")] string? UsuarioNome
);

public record PncpTipoContratoDto(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("nome")] string? Nome
);

public record PncpCategoriaProcessoDto(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("nome")] string? Nome
);

public record PncpAtasResponse(
	[property: JsonPropertyName("data")] List<PncpAtaDto> Data,
	[property: JsonPropertyName("totalRegistros")] long TotalRegistros,
	[property: JsonPropertyName("totalPaginas")] int TotalPaginas,
	[property: JsonPropertyName("numeroPagina")] int NumeroPagina,
	[property: JsonPropertyName("paginasRestantes")] int PaginasRestantes,
	[property: JsonPropertyName("empty")] bool Empty
);

public record PncpAtaDto(
	[property: JsonPropertyName("numeroControlePNCPAta")] string NumeroControlePncpAta,
	[property: JsonPropertyName("numeroControlePNCPCompra")] string? NumeroControlePncpCompra,
	[property: JsonPropertyName("numeroAtaRegistroPreco")] string? NumeroAtaRegistroPreco,
	[property: JsonPropertyName("anoAta")] int AnoAta,
	[property: JsonPropertyName("objetoContratacao")] string? ObjetoContratacao,
	[property: JsonPropertyName("cnpjOrgao")] string CnpjOrgao,
	[property: JsonPropertyName("nomeOrgao")] string? NomeOrgao,
	[property: JsonPropertyName("cancelado")] bool Cancelado,
	[property: JsonPropertyName("dataCancelamento")] DateTime? DataCancelamento,
	[property: JsonPropertyName("dataAssinatura")] DateTime? DataAssinatura,
	[property: JsonPropertyName("vigenciaInicio")] DateTime? VigenciaInicio,
	[property: JsonPropertyName("vigenciaFim")] DateTime? VigenciaFim,
	[property: JsonPropertyName("dataPublicacaoPncp")] DateTime? DataPublicacaoPncp,
	[property: JsonPropertyName("dataInclusao")] DateTime? DataInclusao,
	[property: JsonPropertyName("dataAtualizacao")] DateTime? DataAtualizacao,
	[property: JsonPropertyName("dataAtualizacaoGlobal")] DateTime? DataAtualizacaoGlobal,
	[property: JsonPropertyName("usuario")] string? Usuario
);
