using System;
using EconomIA.Common.EntityFramework.Mappings;
using EconomIA.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Adapters.Persistence.Repositories.Contratos;

public class ContratoMapping : AggregateMapping<Contrato> {
	public override void Configure(EntityTypeBuilder<Contrato> builder) {
		base.Configure(builder);

		builder.ToTable("contrato");

		builder.Property(x => x.Id)
			.HasColumnName("identificador");

		builder.Property(x => x.IdentificadorDoOrgao)
			.HasColumnName("identificador_do_orgao")
			.IsRequired();

		builder.Property(x => x.NumeroControlePncp)
			.HasColumnName("numero_controle_pncp")
			.HasMaxLength(100)
			.IsRequired();

		builder.Property(x => x.NumeroControlePncpCompra)
			.HasColumnName("numero_controle_pncp_compra")
			.HasMaxLength(100)
			.IsRequired(false);

		builder.Property(x => x.AnoContrato)
			.HasColumnName("ano_contrato")
			.IsRequired();

		builder.Property(x => x.SequencialContrato)
			.HasColumnName("sequencial_contrato")
			.IsRequired();

		builder.Property(x => x.NumeroContratoEmpenho)
			.HasColumnName("numero_contrato_empenho")
			.HasMaxLength(200)
			.IsRequired(false);

		builder.Property(x => x.Processo)
			.HasColumnName("processo")
			.HasMaxLength(200)
			.IsRequired(false);

		builder.Property(x => x.ObjetoContrato)
			.HasColumnName("objeto_contrato")
			.IsRequired(false);

		builder.Property(x => x.TipoContratoId)
			.HasColumnName("tipo_contrato_id")
			.IsRequired(false);

		builder.Property(x => x.TipoContratoNome)
			.HasColumnName("tipo_contrato_nome")
			.HasMaxLength(200)
			.IsRequired(false);

		builder.Property(x => x.CategoriaProcessoId)
			.HasColumnName("categoria_processo_id")
			.IsRequired(false);

		builder.Property(x => x.CategoriaProcessoNome)
			.HasColumnName("categoria_processo_nome")
			.HasMaxLength(200)
			.IsRequired(false);

		builder.Property(x => x.NiFornecedor)
			.HasColumnName("ni_fornecedor")
			.HasMaxLength(20)
			.IsRequired(false);

		builder.Property(x => x.NomeRazaoSocialFornecedor)
			.HasColumnName("nome_razao_social_fornecedor")
			.HasMaxLength(500)
			.IsRequired(false);

		builder.Property(x => x.TipoPessoa)
			.HasColumnName("tipo_pessoa")
			.HasMaxLength(10)
			.IsRequired(false);

		builder.Property(x => x.ValorInicial)
			.HasColumnName("valor_inicial")
			.HasPrecision(18, 4)
			.IsRequired(false);

		builder.Property(x => x.ValorGlobal)
			.HasColumnName("valor_global")
			.HasPrecision(18, 4)
			.IsRequired(false);

		builder.Property(x => x.ValorParcela)
			.HasColumnName("valor_parcela")
			.HasPrecision(18, 4)
			.IsRequired(false);

		builder.Property(x => x.ValorAcumulado)
			.HasColumnName("valor_acumulado")
			.HasPrecision(18, 4)
			.IsRequired(false);

		builder.Property(x => x.NumeroParcelas)
			.HasColumnName("numero_parcelas")
			.IsRequired(false);

		builder.Property(x => x.DataAssinatura)
			.HasColumnName("data_assinatura")
			.IsRequired(false);

		builder.Property(x => x.DataVigenciaInicio)
			.HasColumnName("data_vigencia_inicio")
			.IsRequired(false);

		builder.Property(x => x.DataVigenciaFim)
			.HasColumnName("data_vigencia_fim")
			.IsRequired(false);

		builder.Property(x => x.DataPublicacaoPncp)
			.HasColumnName("data_publicacao_pncp")
			.IsRequired(false);

		builder.Property(x => x.DataAtualizacao)
			.HasColumnName("data_atualizacao")
			.IsRequired(false);

		builder.Property(x => x.DataAtualizacaoGlobal)
			.HasColumnName("data_atualizacao_global")
			.IsRequired(false);

		builder.Property(x => x.Receita)
			.HasColumnName("receita")
			.HasDefaultValue(false);

		builder.Property(x => x.InformacaoComplementar)
			.HasColumnName("informacao_complementar")
			.IsRequired(false);

		builder.Property(x => x.UsuarioNome)
			.HasColumnName("usuario_nome")
			.HasMaxLength(200)
			.IsRequired(false);

		builder.Property(x => x.CriadoEm)
			.HasColumnName("criado_em")
			.IsRequired(false);

		builder.Property(x => x.AtualizadoEm)
			.HasColumnName("atualizado_em")
			.IsRequired(false);

		builder.HasIndex(x => x.NumeroControlePncp)
			.IsUnique()
			.HasDatabaseName("contrato_numero_controle_pncp_key");

		builder.HasIndex(x => x.AnoContrato)
			.HasDatabaseName("idx_contrato_ano");

		builder.HasIndex(x => x.NiFornecedor)
			.HasDatabaseName("idx_contrato_fornecedor");

		builder.HasIndex(x => x.IdentificadorDoOrgao)
			.HasDatabaseName("idx_contrato_orgao");

		builder.HasOne(x => x.Orgao)
			.WithMany()
			.HasForeignKey(x => x.IdentificadorDoOrgao)
			.OnDelete(DeleteBehavior.Restrict);

		builder.Ignore(x => x.DomainEvents);
	}
}
