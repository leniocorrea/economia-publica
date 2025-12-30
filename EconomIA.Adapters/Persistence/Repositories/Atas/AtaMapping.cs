using System;
using EconomIA.Common.EntityFramework.Mappings;
using EconomIA.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Adapters.Persistence.Repositories.Atas;

public class AtaMapping : AggregateMapping<Ata> {
	public override void Configure(EntityTypeBuilder<Ata> builder) {
		base.Configure(builder);

		builder.ToTable("ata");

		builder.Property(x => x.Id)
			.HasColumnName("identificador");

		builder.Property(x => x.IdentificadorDoOrgao)
			.HasColumnName("identificador_do_orgao")
			.IsRequired();

		builder.Property(x => x.NumeroControlePncpAta)
			.HasColumnName("numero_controle_pncp_ata")
			.HasMaxLength(100)
			.IsRequired();

		builder.Property(x => x.NumeroControlePncpCompra)
			.HasColumnName("numero_controle_pncp_compra")
			.HasMaxLength(100)
			.IsRequired(false);

		builder.Property(x => x.NumeroAtaRegistroPreco)
			.HasColumnName("numero_ata_registro_preco")
			.HasMaxLength(100)
			.IsRequired(false);

		builder.Property(x => x.AnoAta)
			.HasColumnName("ano_ata")
			.IsRequired();

		builder.Property(x => x.ObjetoContratacao)
			.HasColumnName("objeto_contratacao")
			.IsRequired(false);

		builder.Property(x => x.Cancelado)
			.HasColumnName("cancelado")
			.HasDefaultValue(false);

		builder.Property(x => x.DataCancelamento)
			.HasColumnName("data_cancelamento")
			.IsRequired(false);

		builder.Property(x => x.DataAssinatura)
			.HasColumnName("data_assinatura")
			.IsRequired(false);

		builder.Property(x => x.VigenciaInicio)
			.HasColumnName("vigencia_inicio")
			.IsRequired(false);

		builder.Property(x => x.VigenciaFim)
			.HasColumnName("vigencia_fim")
			.IsRequired(false);

		builder.Property(x => x.DataPublicacaoPncp)
			.HasColumnName("data_publicacao_pncp")
			.IsRequired(false);

		builder.Property(x => x.DataInclusao)
			.HasColumnName("data_inclusao")
			.IsRequired(false);

		builder.Property(x => x.DataAtualizacao)
			.HasColumnName("data_atualizacao")
			.IsRequired(false);

		builder.Property(x => x.DataAtualizacaoGlobal)
			.HasColumnName("data_atualizacao_global")
			.IsRequired(false);

		builder.Property(x => x.Usuario)
			.HasColumnName("usuario")
			.HasMaxLength(200)
			.IsRequired(false);

		builder.Property(x => x.CriadoEm)
			.HasColumnName("criado_em")
			.IsRequired(false);

		builder.Property(x => x.AtualizadoEm)
			.HasColumnName("atualizado_em")
			.IsRequired(false);

		builder.HasIndex(x => x.NumeroControlePncpAta)
			.IsUnique()
			.HasDatabaseName("ata_numero_controle_pncp_ata_key");

		builder.HasIndex(x => x.AnoAta)
			.HasDatabaseName("idx_ata_ano");

		builder.HasIndex(x => x.NumeroControlePncpCompra)
			.HasDatabaseName("idx_ata_compra");

		builder.HasIndex(x => x.IdentificadorDoOrgao)
			.HasDatabaseName("idx_ata_orgao");

		builder.HasOne(x => x.Orgao)
			.WithMany()
			.HasForeignKey(x => x.IdentificadorDoOrgao)
			.OnDelete(DeleteBehavior.Restrict);

		builder.Ignore(x => x.DomainEvents);
	}
}
