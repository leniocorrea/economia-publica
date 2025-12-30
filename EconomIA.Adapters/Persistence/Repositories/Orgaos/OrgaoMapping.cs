using System;
using EconomIA.Common.EntityFramework.Mappings;
using EconomIA.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Adapters.Persistence.Repositories.Orgaos;

public class OrgaoMapping : AggregateMapping<Orgao> {
	public override void Configure(EntityTypeBuilder<Orgao> builder) {
		base.Configure(builder);

		builder.ToTable("orgao");

		builder.Property(x => x.Cnpj)
			.HasColumnName("cnpj")
			.HasMaxLength(20)
			.IsRequired();

		builder.Property(x => x.RazaoSocial)
			.HasColumnName("razao_social")
			.HasMaxLength(500)
			.IsRequired();

		builder.Property(x => x.CriadoEm)
			.HasColumnName("criado_em")
			.IsRequired();

		builder.Property(x => x.AtualizadoEm)
			.HasColumnName("atualizado_em")
			.IsRequired();

		builder.Property(x => x.NomeFantasia)
			.HasColumnName("nome_fantasia")
			.HasMaxLength(500)
			.IsRequired(false);

		builder.Property(x => x.CodigoNaturezaJuridica)
			.HasColumnName("codigo_natureza_juridica")
			.HasMaxLength(10)
			.IsRequired(false);

		builder.Property(x => x.DescricaoNaturezaJuridica)
			.HasColumnName("descricao_natureza_juridica")
			.HasMaxLength(500)
			.IsRequired(false);

		builder.Property(x => x.PoderId)
			.HasColumnName("poder_id")
			.HasMaxLength(5)
			.IsRequired(false);

		builder.Property(x => x.EsferaId)
			.HasColumnName("esfera_id")
			.HasMaxLength(5)
			.IsRequired(false);

		builder.Property(x => x.SituacaoCadastral)
			.HasColumnName("situacao_cadastral")
			.HasMaxLength(50)
			.IsRequired(false);

		builder.Property(x => x.MotivoSituacaoCadastral)
			.HasColumnName("motivo_situacao_cadastral")
			.HasMaxLength(500)
			.IsRequired(false);

		builder.Property(x => x.DataSituacaoCadastral)
			.HasColumnName("data_situacao_cadastral")
			.IsRequired(false);

		builder.Property(x => x.DataValidacao)
			.HasColumnName("data_validacao")
			.IsRequired(false);

		builder.Property(x => x.Validado)
			.HasColumnName("validado")
			.HasDefaultValue(false);

		builder.Property(x => x.DataInclusaoPncp)
			.HasColumnName("data_inclusao_pncp")
			.IsRequired(false);

		builder.Property(x => x.DataAtualizacaoPncp)
			.HasColumnName("data_atualizacao_pncp")
			.IsRequired(false);

		builder.Property(x => x.StatusAtivo)
			.HasColumnName("status_ativo")
			.HasDefaultValue(true);

		builder.Property(x => x.JustificativaAtualizacao)
			.HasColumnName("justificativa_atualizacao")
			.HasMaxLength(1000)
			.IsRequired(false);

		builder.HasIndex(x => x.Cnpj)
			.IsUnique()
			.HasDatabaseName("idx_orgao_cnpj");

		builder.HasMany(x => x.Unidades)
			.WithOne(x => x.Orgao)
			.HasForeignKey(x => x.IdentificadorDoOrgao)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Ignore(x => x.DomainEvents);
	}
}
