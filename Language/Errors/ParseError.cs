namespace Cranberry.Errors;

public class ParseError(string message, Token? token = null) : Exception(message) {
	// Token can be null (EOF) so make it nullable.
	public Token? Token = token;
	public bool FullLine = false;

	public override string ToString() {
		try {
			if (Token == null) return $"ParseError: {Message}";

			var lineStr = Token.Line >= 0 ? Token.Line.ToString() : "unknown";
			var fileStr = string.IsNullOrEmpty(Token.FileName) ? "unknown" : Token.FileName;

			return $"ParseError at line {lineStr}, file `{fileStr}`: {Message}";
		} catch (Exception ex) {
			return $"ParseError: {Message} (failed to format token: {ex.Message})";
		}
	}
}