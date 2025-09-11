namespace Cranberry;

public class Program {
	public readonly Parser parser;
	public readonly Interpreter interpreter = new ();

	public Program(string text) {
		var tokens = new Lexer(text).GetTokens();
		// Lexer.PrintTokens(tokens);
		parser = new Parser(tokens.ToArray());
	}
	
	public void RunProgram() {
		while (parser.PeekAhead() != null) {
			interpreter.Evaluate(parser.Parse());
			if (parser.Check(";")) {
				parser.Advance();
			}
		}
	}
}