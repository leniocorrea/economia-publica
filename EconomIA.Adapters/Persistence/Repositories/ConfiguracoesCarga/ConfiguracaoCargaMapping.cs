using EconomIA.Common.EntityFramework.Mappings;
using EconomIA.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Adapters.Persistence.Repositories.ConfiguracoesCarga;

public class ConfiguracaoCargaMapping : AggregateMapping<ConfiguracaoCarga> {
	public override void Configure(EntityTypeBuilder<ConfiguracaoCarga> builder) {
		base.Configure(builder);

		builder.ToTable("configuracao_carga");

		builder.Property(x => x.HorarioExecucao)
			.HasColumnName("horario_execucao")
			.IsRequired();

		builder.Property(x => x.DiasSemana)
			.HasColumnName("dias_semana")
			.IsRequired();

		builder.Property(x => x.Habilitado)
			.HasColumnName("habilitado")
			.HasDefaultValue(true);

		builder.Property(x => x.DiasRetroativos)
			.HasColumnName("dias_retroativos")
			.HasDefaultValue(1);

		builder.Property(x => x.DiasCargaInicial)
			.HasColumnName("dias_carga_inicial")
			.HasDefaultValue(90);

		builder.Property(x => x.MaxConcorrencia)
			.HasColumnName("max_concorrencia")
			.HasDefaultValue(4);

		builder.Property(x => x.CarregarCompras)
			.HasColumnName("carregar_compras")
			.HasDefaultValue(true);

		builder.Property(x => x.CarregarContratos)
			.HasColumnName("carregar_contratos")
			.HasDefaultValue(true);

		builder.Property(x => x.CarregarAtas)
			.HasColumnName("carregar_atas")
			.HasDefaultValue(true);

		builder.Property(x => x.SincronizarOrgaos)
			.HasColumnName("sincronizar_orgaos")
			.HasDefaultValue(true);

		builder.Property(x => x.HorarioSincronizacao)
			.HasColumnName("horario_sincronizacao")
			.IsRequired();

		builder.Property(x => x.DiaSemanasSincronizacao)
			.HasColumnName("dia_semana_sincronizacao")
			.HasDefaultValue(0);

		builder.Property(x => x.AtualizadoEm)
			.HasColumnName("atualizado_em")
			.IsRequired();

		builder.Property(x => x.AtualizadoPor)
			.HasColumnName("atualizado_por")
			.HasMaxLength(100);

		builder.Ignore(x => x.DomainEvents);
	}
}
