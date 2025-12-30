using System;

namespace EconomIA.Common.Persistence;

public enum RepositoryErrorCode {
	NotFound,
	MultipleFound,
	MissingArgument,
	InvalidFormat,
	Unknown,
}

public record RepositoryError(RepositoryErrorCode Code, String Message) {
	public static RepositoryError NotFound(String message) => new(RepositoryErrorCode.NotFound, message);
	public static RepositoryError MultipleFound(String message) => new(RepositoryErrorCode.MultipleFound, message);
	public static RepositoryError MissingArgument(String message) => new(RepositoryErrorCode.MissingArgument, message);
	public static RepositoryError InvalidFormat(String message) => new(RepositoryErrorCode.InvalidFormat, message);
	public static RepositoryError Unknown(String message) => new(RepositoryErrorCode.Unknown, message);
}
