namespace Cranberry.Errors;

public class ParseError(string message, int position) : Exception(message) {
	public int Position { get; } = position;

	public override string ToString()
	{
		return $"ParseError at token {Position}: {Message}";
	}
}