using System;
using EconomIA.Common.EntityFramework.Mappings;
using EconomIA.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Adapters.Persistence.Repositories.Orgaos;

public class UnidadeMapping : EntityMapping<Unidade> {
	public override void Configure(EntityTypeBuilder<Unidade> builder) {
		base.Configure(builder);

		builder.ToTable("unidade");

		builder.Property(x => x.IdentificadorDoOrgao)
			.HasColumnName("identificador_do_orgao")
			.IsRequired();

		builder.Property(x => x.CodigoUnidade)
			.HasColumnName("codigo_unidade")
			.HasMaxLength(50)
			.IsRequired();

		builder.Property(x => x.NomeUnidade)
			.HasColumnName("nome_unidade")
			.HasMaxLength(500)
			.IsRequired();

		builder.Property(x => x.MunicipioNome)
			.HasColumnName("municipio_nome")
			.HasMaxLength(200)
			.IsRequired(false);

		builder.Property(x => x.MunicipioCodigoIbge)
			.HasColumnName("municipio_codigo_ibge")
			.HasMaxLength(10)
			.IsRequired(false);

		builder.Property(x => x.UfSigla)
			.HasColumnName("uf_sigla")
			.HasMaxLength(2)
			.IsRequired(false);

		builder.Property(x => x.UfNome)
			.HasColumnName("uf_nome")
			.HasMaxLength(50)
			.IsRequired(false);

		builder.Property(x => x.StatusAtivo)
			.HasColumnName("status_ativo")
			.HasDefaultValue(true);

		builder.Property(x => x.DataInclusaoPncp)
			.HasColumnName("data_inclusao_pncp")
			.IsRequired(false);

		builder.Property(x => x.DataAtualizacaoPncp)
			.HasColumnName("data_atualizacao_pncp")
			.IsRequired(false);

		builder.Property(x => x.JustificativaAtualizacao)
			.HasColumnName("justificativa_atualizacao")
			.HasMaxLength(1000)
			.IsRequired(false);

		builder.Property(x => x.CriadoEm)
			.HasColumnName("criado_em")
			.IsRequired();

		builder.Property(x => x.AtualizadoEm)
			.HasColumnName("atualizado_em")
			.IsRequired();

		builder.HasIndex(x => new { x.IdentificadorDoOrgao, x.CodigoUnidade })
			.IsUnique()
			.HasDatabaseName("idx_unidade_orgao_codigo");

		builder.HasIndex(x => x.UfSigla)
			.HasDatabaseName("idx_unidade_uf");

		builder.HasIndex(x => x.MunicipioCodigoIbge)
			.HasDatabaseName("idx_unidade_municipio");
	}
}
