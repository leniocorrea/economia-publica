using System;
using EconomIA.Common.EntityFramework.Mappings;
using EconomIA.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Adapters.Persistence.Repositories.ItensDaCompra;

public class ItemDaCompraMapping : AggregateMapping<ItemDaCompra> {
	public override void Configure(EntityTypeBuilder<ItemDaCompra> builder) {
		base.Configure(builder);

		builder.ToTable("item_da_compra");

		builder.Property(x => x.Id)
			.HasColumnName("identificador");

		builder.Property(x => x.IdentificadorDaCompra)
			.HasColumnName("identificador_da_compra")
			.IsRequired();

		builder.Property(x => x.NumeroItem)
			.HasColumnName("numero_item")
			.IsRequired();

		builder.Property(x => x.Descricao)
			.HasColumnName("descricao")
			.IsRequired(false);

		builder.Property(x => x.Quantidade)
			.HasColumnName("quantidade")
			.HasPrecision(18, 4)
			.IsRequired(false);

		builder.Property(x => x.UnidadeMedida)
			.HasColumnName("unidade_medida")
			.HasMaxLength(100)
			.IsRequired(false);

		builder.Property(x => x.ValorUnitarioEstimado)
			.HasColumnName("valor_unitario_estimado")
			.HasPrecision(18, 4)
			.IsRequired(false);

		builder.Property(x => x.ValorTotal)
			.HasColumnName("valor_total")
			.HasPrecision(18, 2)
			.IsRequired(false);

		builder.Property(x => x.CriterioJulgamentoNome)
			.HasColumnName("criterio_julgamento_nome")
			.HasMaxLength(200)
			.IsRequired(false);

		builder.Property(x => x.SituacaoCompraItemNome)
			.HasColumnName("situacao_compra_item_nome")
			.HasMaxLength(200)
			.IsRequired(false);

		builder.Property(x => x.TemResultado)
			.HasColumnName("tem_resultado")
			.HasDefaultValue(false);

		builder.Property(x => x.DataAtualizacao)
			.HasColumnName("data_atualizacao")
			.IsRequired(false);

		builder.Property(x => x.CriadoEm)
			.HasColumnName("criado_em")
			.IsRequired();

		builder.Property(x => x.AtualizadoEm)
			.HasColumnName("atualizado_em")
			.IsRequired();

		builder.HasIndex(x => new { x.IdentificadorDaCompra, x.NumeroItem })
			.IsUnique()
			.HasDatabaseName("idx_item_compra_unique");

		builder.Ignore(x => x.DomainEvents);
	}
}
