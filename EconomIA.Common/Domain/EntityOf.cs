using System;

namespace EconomIA.Common.Domain;

public interface IEntity;

public abstract class Entity : IEntity, IEquatable<Entity> {
	protected Entity() {
	}

	protected Entity(Int64 id) {
		Id = id;
	}

	public virtual Int64 Id { get; protected set; }

	public override Boolean Equals(Object? otherObject) => Equals(otherObject as Entity);

	public Boolean Equals(Entity? otherEntity) {
		if (otherEntity is null) {
			return false;
		}

		if (ReferenceEquals(this, otherEntity)) {
			return true;
		}

		if (GetType() != otherEntity.GetType()) {
			return false;
		}

		if (IsTransient() || otherEntity.IsTransient()) {
			return false;
		}

		return Id.Equals(otherEntity.Id);
	}

	protected Boolean IsTransient() => Id.Equals(default);

	public override Int32 GetHashCode() {
		if (IsTransient()) {
			return base.GetHashCode();
		}

		var hash = new HashCode();
		hash.Add(GetType());
		hash.Add(Id);

		return hash.ToHashCode();
	}

	public static Boolean operator ==(Entity? x, Entity? y) {
		if (ReferenceEquals(x, y)) {
			return true;
		}

		if (x is null || y is null) {
			return false;
		}

		return x.Equals(y);
	}

	public static Boolean operator !=(Entity? x, Entity? y) => !(x == y);
}
