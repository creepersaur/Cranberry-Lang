namespace Cranberry.Errors;

public class RuntimeError(string message, Token? start_token = null) : Exception(message) {
	public Token? StartToken = start_token;
	public override string ToString() => $"RuntimeError: {Message}";
}

public class ExecutionError(Token start_token, string message) : Exception(message) {
	public readonly Token StartToken = start_token;
	public override string ToString() => $"ExecutionError: {Message}";
}