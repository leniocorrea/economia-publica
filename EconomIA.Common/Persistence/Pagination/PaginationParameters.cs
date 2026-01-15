using System;
using CSharpFunctionalExtensions;
using EconomIA.Common.Domain;
using static CSharpFunctionalExtensions.Result;

namespace EconomIA.Common.Persistence.Pagination;

public sealed class PaginationParameters {
	private static Int32 DefaultLimit => 50;
	private static Int32 MaxLimit => 1_000;
	public static Int32 MaxFieldCount => 10;

	public static Result<PaginationParameters> Create(String? order = null, String? cursor = null, Int32? limit = null) {
		if (limit is < 1) {
			return Failure<PaginationParameters>($"{limit} is not a valid limit.");
		}

		if (limit is not null && limit > MaxLimit) {
			return Failure<PaginationParameters>($"Limit cannot be greater than {MaxLimit}.");
		}

		if (String.IsNullOrWhiteSpace(order)) {
			order = $"+{nameof(Aggregate.Id)}";
		}

		return new PaginationParameters(order?.Trim(), cursor?.Trim(), limit ?? DefaultLimit);
	}

	private PaginationParameters(String? order, String? cursor, Int32 limit) {
		Order = order;
		Cursor = cursor;
		Limit = limit;
	}

	public String? Order { get; }
	public String? Cursor { get; }
	public Int32 Limit { get; }

	public Result<PaginationParameters> NextPage(String cursor) {
		if (String.IsNullOrWhiteSpace(cursor)) {
			return Failure<PaginationParameters>("Cursor is required.");
		}

		return Create(cursor: cursor, limit: Limit);
	}
}
