namespace Cranberry.Errors;

public class ReturnException(object? value) : Exception {
	public object? Value { get; set; } = value;
}

public class BreakException(object? value, Token? start_token = null) : Exception {
	public Token? StartToken = start_token;
	public object? Value { get; set; } = value;
}

public class OutException(object? value) : Exception {
	public object? Value { get; set; } = value;
}

public class ContinueException(Token? start_token = null) : Exception {
	public Token? StartToken = start_token;
}