namespace Cranberry.Errors;

public class IncludeFileException(object path) : Exception {
	public object Path = path;
}