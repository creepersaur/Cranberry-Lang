using Cranberry.Errors;

namespace Cranberry;

public class Program {
	public readonly Parser parser;
	public readonly Interpreter interpreter = new();

	public Program(string text) {
		var tokens = new Lexer(text).GetTokens();
		// Lexer.PrintTokens(tokens);
		parser = new Parser(tokens.ToArray());
	}

	public void RunProgram() {
		while (parser.PeekAhead() != null) {
			try {
				interpreter.Evaluate(parser.Parse());
			} catch (ReturnException) {
				throw new RuntimeError("Cannot `return` in main scope.");
			} catch (OutException) {
				throw new RuntimeError("Cannot `out` in main scope.");
			} catch (BreakException) {
				throw new RuntimeError("`break` must only be used in loops.");
			}

			if (parser.Check(";")) {
				parser.Advance();
			}
		}
	}
}