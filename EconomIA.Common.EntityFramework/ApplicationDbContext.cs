using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EconomIA.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EconomIA.Common.EntityFramework;

public abstract class ApplicationDbContext(DbContextOptions options) : DbContext(options) {
	protected override void OnModelCreating(ModelBuilder builder) {
		base.OnModelCreating(builder);

		ConfigureMappings(builder);
		ConfigureNamingConventions(builder);
	}

	private void ConfigureMappings(ModelBuilder builder) {
		IgnoreAbstractClasses(builder);

		foreach (var assembly in GetAssemblies()) {
			builder.ApplyConfigurationsFromAssembly(assembly);
		}
	}

	private void IgnoreAbstractClasses(ModelBuilder builder) {
		builder.Ignore(typeof(Entity));
		builder.Ignore(typeof(Aggregate));
	}

	private IEnumerable<Assembly> GetAssemblies() {
		var thisAssembly = Assembly.GetExecutingAssembly();
		yield return thisAssembly;

		var otherAssembly = GetType().Assembly;

		if (otherAssembly != thisAssembly) {
			yield return otherAssembly;
		}
	}

	private void ConfigureNamingConventions(ModelBuilder builder) {
		foreach (var entity in builder.Model.GetEntityTypes()) {
			var tableName = entity.GetTableName();

			if (tableName is null) {
				continue;
			}

			ConfigurePrimaryKeyConvention(entity, tableName);
			ConfigureForeignKeyConventions(entity, tableName);
			ConfigureIndexConventions(entity, tableName);
		}
	}

	private static void ConfigurePrimaryKeyConvention(IMutableEntityType entity, String tableName) {
		var primaryKey = entity.FindPrimaryKey();
		primaryKey?.SetName($"pk_{tableName}");
	}

	private static void ConfigureForeignKeyConventions(IMutableEntityType entity, String tableName) {
		foreach (var foreignKey in entity.GetForeignKeys()) {
			var principalTable = foreignKey.PrincipalEntityType.GetTableName();
			var columns = String.Join("_", foreignKey.Properties.Select(p => p.Name));
			var principalColumns = String.Join("_", foreignKey.PrincipalKey.Properties.Select(p => p.Name));

			foreignKey.SetConstraintName($"fk_{tableName}_{columns}_to_{principalTable}_{principalColumns}");
		}
	}

	private static void ConfigureIndexConventions(IMutableEntityType entity, String tableName) {
		foreach (var index in entity.GetIndexes()) {
			var hasCustomName = index.Name?.StartsWith("un_") == true || index.Name?.StartsWith("ix_") == true;

			if (hasCustomName) {
				continue;
			}

			var columns = String.Join("_", index.Properties.Select(p => p.Name));
			var prefix = index.IsUnique ? "un" : "ix";

			index.SetDatabaseName($"{prefix}_{tableName}_{columns}");
		}
	}
}
