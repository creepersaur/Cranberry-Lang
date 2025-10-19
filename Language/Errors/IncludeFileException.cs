namespace Cranberry.Errors;

public class IncludeFileException(object path, bool recursive) : Exception {
	public readonly object Path = path;
	public readonly bool Recursive = recursive;
}