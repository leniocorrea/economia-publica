using System;
using System.Linq.Expressions;

namespace EconomIA.Common.Domain;

public abstract class Specification<TEntity> where TEntity : Entity {
	public static Specification<TEntity> True { get; } = new TrueSpecification<TEntity>();
	public static Specification<TEntity> False { get; } = new FalseSpecification<TEntity>();

	public abstract Expression<Func<TEntity, Boolean>> Rule();

	public Boolean IsSatisfiedBy(TEntity entity) {
		var predicate = Rule().Compile();
		return predicate(entity);
	}

	public Specification<TEntity> And(Specification<TEntity> specification) {
		if (this == True) {
			return specification;
		}

		if (specification == True) {
			return this;
		}

		return new AndSpecification<TEntity>(this, specification);
	}

	public Specification<TEntity> Or(Specification<TEntity> specification) {
		if (this == True || specification == True) {
			return True;
		}

		return new OrSpecification<TEntity>(this, specification);
	}

	public static Specification<TEntity> operator +(Specification<TEntity> x, Specification<TEntity> y) => (x ?? True).And(y ?? True);
	public static Specification<TEntity> operator |(Specification<TEntity> x, Specification<TEntity> y) => (x ?? True).Or(y ?? True);
	public static Specification<TEntity> operator !(Specification<TEntity> x) => Not(x);

	public static Specification<TEntity> Create(Expression<Func<TEntity, Boolean>> expression) => new ExpressionSpecification<TEntity>(expression);

	public static Specification<TEntity> Not(Specification<TEntity> specification) {
		specification ??= True;
		return new NotSpecification<TEntity>(specification);
	}
}

public class ExpressionSpecification<TEntity>(Expression<Func<TEntity, Boolean>> expression) : Specification<TEntity> where TEntity : Entity {
	public override Expression<Func<TEntity, Boolean>> Rule() => expression;
}

internal sealed class TrueSpecification<TEntity> : Specification<TEntity> where TEntity : Entity {
	public override Expression<Func<TEntity, Boolean>> Rule() => x => true;
}

internal sealed class FalseSpecification<TEntity> : Specification<TEntity> where TEntity : Entity {
	public override Expression<Func<TEntity, Boolean>> Rule() => x => false;
}

internal sealed class AndSpecification<TEntity>(Specification<TEntity> left, Specification<TEntity> right) : Specification<TEntity> where TEntity : Entity {
	public override Expression<Func<TEntity, Boolean>> Rule() {
		var parameter = Expression.Parameter(typeof(TEntity), "x");
		var leftExpression = left.Rule();
		var rightExpression = right.Rule();

		var body = Expression.AndAlso(
			Expression.Invoke(leftExpression, parameter),
			Expression.Invoke(rightExpression, parameter));

		var lambda = Expression.Lambda<Func<TEntity, Boolean>>(body, parameter);
		return lambda;
	}
}

internal sealed class OrSpecification<TEntity>(Specification<TEntity> left, Specification<TEntity> right) : Specification<TEntity> where TEntity : Entity {
	public override Expression<Func<TEntity, Boolean>> Rule() {
		var parameter = Expression.Parameter(typeof(TEntity), "x");
		var leftExpression = left.Rule();
		var rightExpression = right.Rule();

		var body = Expression.OrElse(
			Expression.Invoke(leftExpression, parameter),
			Expression.Invoke(rightExpression, parameter));

		var lambda = Expression.Lambda<Func<TEntity, Boolean>>(body, parameter);
		return lambda;
	}
}

internal sealed class NotSpecification<TEntity>(Specification<TEntity> specification) : Specification<TEntity> where TEntity : Entity {
	public override Expression<Func<TEntity, Boolean>> Rule() {
		var parameter = Expression.Parameter(typeof(TEntity), "x");
		var expression = specification.Rule();
		var body = Expression.Not(Expression.Invoke(expression, parameter));
		var lambda = Expression.Lambda<Func<TEntity, Boolean>>(body, parameter);
		return lambda;
	}
}

public static class SpecificationExtensions {
	public static Specification<TEntity> Combine<TEntity>(params Specification<TEntity>[] specifications) where TEntity : Entity {
		var combined = Specification<TEntity>.True;

		foreach (var specification in specifications) {
			if (specification is not null) {
				combined += specification;
			}
		}

		return combined;
	}
}
