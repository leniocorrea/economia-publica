using System;

namespace EconomIA.Common.Domain;

public abstract class DomainEvent {
	public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
