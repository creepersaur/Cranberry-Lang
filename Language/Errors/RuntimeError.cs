namespace Cranberry.Errors;

public class RuntimeError(string message, int? position = null) : Exception(message) {
	public int? Position { get; } = position;

	public override string ToString() {
		return Position.HasValue ?
			$"RuntimeError at token {Position.Value}: {Message}"
			: $"RuntimeError: {Message}";
	}
}