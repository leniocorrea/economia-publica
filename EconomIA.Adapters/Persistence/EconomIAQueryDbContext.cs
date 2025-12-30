using System;
using EconomIA.Common.EntityFramework;
using EconomIA.Domain;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Adapters.Persistence;

public class EconomIAQueryDbContext(DbContextOptions<EconomIAQueryDbContext> options) : QueryDbContext(options) {
	public DbSet<Orgao> Orgaos { get; set; } = null!;
	public DbSet<Unidade> Unidades { get; set; } = null!;
	public DbSet<Compra> Compras { get; set; } = null!;
	public DbSet<ItemDaCompra> ItensDaCompra { get; set; } = null!;
	public DbSet<Ata> Atas { get; set; } = null!;
	public DbSet<Contrato> Contratos { get; set; } = null!;
}
