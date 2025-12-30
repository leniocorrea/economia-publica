using System;
using EconomIA.Common.EntityFramework.Mappings;
using EconomIA.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Adapters.Persistence.Repositories.Compras;

public class CompraMapping : AggregateMapping<Compra> {
	public override void Configure(EntityTypeBuilder<Compra> builder) {
		base.Configure(builder);

		builder.ToTable("compra");

		builder.Property(x => x.Id)
			.HasColumnName("identificador");

		builder.Property(x => x.IdentificadorDoOrgao)
			.HasColumnName("identificador_do_orgao")
			.IsRequired();

		builder.Property(x => x.NumeroControlePncp)
			.HasColumnName("numero_controle_pncp")
			.HasMaxLength(100)
			.IsRequired();

		builder.Property(x => x.AnoCompra)
			.HasColumnName("ano_compra")
			.IsRequired();

		builder.Property(x => x.SequencialCompra)
			.HasColumnName("sequencial_compra")
			.IsRequired();

		builder.Property(x => x.ModalidadeIdentificador)
			.HasColumnName("modalidade_identificador")
			.IsRequired();

		builder.Property(x => x.ModalidadeNome)
			.HasColumnName("modalidade_nome")
			.HasMaxLength(200)
			.IsRequired(false);

		builder.Property(x => x.ObjetoCompra)
			.HasColumnName("objeto_compra")
			.IsRequired(false);

		builder.Property(x => x.ValorTotalEstimado)
			.HasColumnName("valor_total_estimado")
			.HasPrecision(18, 2)
			.IsRequired(false);

		builder.Property(x => x.ValorTotalHomologado)
			.HasColumnName("valor_total_homologado")
			.HasPrecision(18, 2)
			.IsRequired(false);

		builder.Property(x => x.SituacaoCompraNome)
			.HasColumnName("situacao_compra_nome")
			.HasMaxLength(200)
			.IsRequired(false);

		builder.Property(x => x.DataAberturaProposta)
			.HasColumnName("data_abertura_proposta")
			.IsRequired(false);

		builder.Property(x => x.DataEncerramentoProposta)
			.HasColumnName("data_encerramento_proposta")
			.IsRequired(false);

		builder.Property(x => x.AmparoLegalNome)
			.HasColumnName("amparo_legal_nome")
			.HasMaxLength(500)
			.IsRequired(false);

		builder.Property(x => x.ModoDisputaNome)
			.HasColumnName("modo_disputa_nome")
			.HasMaxLength(200)
			.IsRequired(false);

		builder.Property(x => x.LinkPncp)
			.HasColumnName("link_pncp")
			.HasMaxLength(500)
			.IsRequired(false);

		builder.Property(x => x.DataAtualizacaoGlobal)
			.HasColumnName("data_atualizacao_global")
			.IsRequired(false);

		builder.Property(x => x.ItensCarregados)
			.HasColumnName("itens_carregados")
			.HasDefaultValue(false);

		builder.Property(x => x.CriadoEm)
			.HasColumnName("criado_em")
			.IsRequired();

		builder.Property(x => x.AtualizadoEm)
			.HasColumnName("atualizado_em")
			.IsRequired();

		builder.HasIndex(x => new { x.IdentificadorDoOrgao, x.AnoCompra, x.SequencialCompra })
			.IsUnique()
			.HasDatabaseName("idx_compra_unique");

		builder.HasOne(x => x.Orgao)
			.WithMany()
			.HasForeignKey(x => x.IdentificadorDoOrgao)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasMany(x => x.ItensDaCompra)
			.WithOne(x => x.Compra)
			.HasForeignKey(x => x.IdentificadorDaCompra)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Ignore(x => x.DomainEvents);
	}
}
