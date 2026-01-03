using EconomIA.Common.EntityFramework.Mappings;
using EconomIA.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Adapters.Persistence.Repositories.OrgaosMonitorados;

public class OrgaoMonitoradoMapping : AggregateMapping<OrgaoMonitorado> {
	public override void Configure(EntityTypeBuilder<OrgaoMonitorado> builder) {
		base.Configure(builder);

		builder.ToTable("orgao_monitorado");

		builder.Property(x => x.IdentificadorDoOrgao)
			.HasColumnName("identificador_do_orgao")
			.IsRequired();

		builder.Property(x => x.Ativo)
			.HasColumnName("ativo")
			.HasDefaultValue(true);

		builder.Property(x => x.CriadoEm)
			.HasColumnName("criado_em")
			.IsRequired();

		builder.Property(x => x.AtualizadoEm)
			.HasColumnName("atualizado_em")
			.IsRequired();

		builder.HasIndex(x => x.IdentificadorDoOrgao)
			.IsUnique()
			.HasDatabaseName("un_orgao_monitorado_identificador_do_orgao");

		builder.HasOne(x => x.Orgao)
			.WithMany()
			.HasForeignKey(x => x.IdentificadorDoOrgao)
			.OnDelete(DeleteBehavior.Restrict);

		builder.Ignore(x => x.DomainEvents);
	}
}
