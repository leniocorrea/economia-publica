using System;
using EconomIA.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Common.EntityFramework.Mappings;

public abstract class EntityMapping<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : Entity {
	public virtual void Configure(EntityTypeBuilder<TEntity> builder) {
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			.HasColumnName("identificador")
			.ValueGeneratedOnAdd()
			.IsRequired();
	}
}
