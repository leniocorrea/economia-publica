using EconomIA.Common.Domain;

namespace EconomIA.Common.EntityFramework.Mappings;

public abstract class AggregateMapping<TAggregate> : EntityMapping<TAggregate> where TAggregate : Aggregate;
