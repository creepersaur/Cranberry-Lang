namespace Cranberry.Errors;

public class ReturnException(object? value) : Exception {
	public object? Value { get; set; } = value;
}

public class BreakException(object? value) : Exception {
	public object? Value { get; set; } = value;
}

public class OutException(object? value) : Exception {
	public object? Value { get; set; } = value;
}

public class ContinueException : Exception;