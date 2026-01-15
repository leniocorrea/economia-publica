using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EconomIA.Common.Persistence.Pagination;

public class PaginationResult<TItems>(IEnumerable<TItems> items, String? nextCursor, Int64? totalCount = null) {
	public ImmutableArray<TItems> Items { get; } = items?.ToImmutableArray() ?? ImmutableArray<TItems>.Empty;
	public Boolean HasMoreItems => NextCursor is not null;
	public String? NextCursor { get; } = nextCursor;
	public Int64? TotalCount { get; } = totalCount;
}
