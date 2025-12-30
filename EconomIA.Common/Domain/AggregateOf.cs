using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EconomIA.Common.Domain;

public abstract class Aggregate : Entity {
	protected Aggregate() {
	}

	protected Aggregate(Int64 id) : base(id) {
	}

	private readonly List<DomainEvent> domainEvents = new();
	public virtual IReadOnlyCollection<DomainEvent> DomainEvents => domainEvents.ToImmutableList();

	public virtual void ClearDomainEvents() => domainEvents.Clear();

	protected virtual void AddDomainEvents(DomainEvent firstEvent, DomainEvent secondEvent, params IEnumerable<DomainEvent> otherEvents) {
		AddDomainEvent(firstEvent);
		AddDomainEvent(secondEvent);

		foreach (var domainEvent in otherEvents) {
			AddDomainEvent(domainEvent);
		}
	}

	protected virtual void AddDomainEvent(DomainEvent domainEvent) {
		if (domainEvent is null) {
			return;
		}

		domainEvents.Add(domainEvent);
	}
}
