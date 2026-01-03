using System;
using EconomIA.Common.EntityFramework;
using EconomIA.Domain;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Adapters.Persistence;

public class EconomIACommandDbContext(DbContextOptions<EconomIACommandDbContext> options) : ApplicationDbContext(options) {
	public DbSet<Orgao> Orgaos { get; set; } = null!;
	public DbSet<Unidade> Unidades { get; set; } = null!;
	public DbSet<Compra> Compras { get; set; } = null!;
	public DbSet<ItemDaCompra> ItensDaCompra { get; set; } = null!;
	public DbSet<ResultadoDoItem> ResultadosDoItem { get; set; } = null!;
	public DbSet<Ata> Atas { get; set; } = null!;
	public DbSet<Contrato> Contratos { get; set; } = null!;
	public DbSet<OrgaoMonitorado> OrgaosMonitorados { get; set; } = null!;

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
		base.OnConfiguring(optionsBuilder);
		optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
	}
}
