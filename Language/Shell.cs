using System.Text;
using System.Text.RegularExpressions;

namespace Cranberry;

class Shell {
	public static bool multilineActive = false;
	public static char? inString = null;
	static int parenCount = 0;
	static int bracketCount = 0;
	static int braceCount = 0;
	static List<char> quotes = new();
	static string[] keywords = ["let", "const", "enum", "class", "constructor", "using", "namespace", "in", "loop", "while", "for", "break", "return", "out", "continue", "fn", "if", "else", "@", "=>"];
	static string[] constants = ["true", "false", "nil"];
	static Lexer lexer = new("", "<stdio>", "<stdio>", false);

	public static string GetInput() {
		StringBuilder buffer = new StringBuilder();
		int cursorIndex = 0;
		int promptWidth = 4; // Length of ">>> "

		if (multilineActive) {
			Console.ForegroundColor = ConsoleColor.Black;
			Console.Write("... ");
		} else {
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write(">>> ");
		}
		Console.ResetColor();

		while (true) {
			var keyInfo = Console.ReadKey(true);

			// 1. Handle Navigation
			if (keyInfo.Key == ConsoleKey.LeftArrow) {
				cursorIndex = Math.Max(0, cursorIndex - 1);
			} else if (keyInfo.Key == ConsoleKey.RightArrow) {
				cursorIndex = Math.Min(buffer.Length, cursorIndex + 1);
			} else if (keyInfo.Key == ConsoleKey.Home) {
				cursorIndex = 0;
			} else if (keyInfo.Key == ConsoleKey.End) {
				cursorIndex = buffer.Length;
			}

			// 2. Handle Editing
			if (keyInfo.Key == ConsoleKey.Enter) {
				multilineActive = IsIncomplete();
				break;
			}
			if (keyInfo.Key == ConsoleKey.Backspace && cursorIndex > 0) {
				buffer.Remove(cursorIndex - 1, 1);
				cursorIndex--;
			} else if (keyInfo.Key == ConsoleKey.Delete && cursorIndex < buffer.Length) {
				buffer.Remove(cursorIndex, 1);
			} else if (keyInfo.Key == ConsoleKey.Tab) {
				const int tabSize = 4;
				int spacesToAdd = tabSize - (cursorIndex % tabSize);

				for (int i = 0; i < spacesToAdd; i++) {
					buffer.Insert(cursorIndex, ' ');
					cursorIndex++;
				}
			} else if (!char.IsControl(keyInfo.KeyChar)) {
				buffer.Insert(cursorIndex, keyInfo.KeyChar);
				cursorIndex++;
			}

			// Handle multi-lines
			if (keyInfo.KeyChar == '(') {
				parenCount++;
			} else if (keyInfo.KeyChar == ')') {
				parenCount--;
			}

			if (keyInfo.KeyChar == '[') {
				bracketCount++;
			} else if (keyInfo.KeyChar == ']') {
				bracketCount--;
			}

			if (keyInfo.KeyChar == '{') {
				braceCount++;
			} else if (keyInfo.KeyChar == '}') {
				braceCount--;
			}

			if (keyInfo.KeyChar == '"' || keyInfo.KeyChar == '\'' || keyInfo.KeyChar == '`') {
				if (quotes.Contains(keyInfo.KeyChar)) {
					quotes.Remove(keyInfo.KeyChar);
				} else if (quotes.Count == 0) {
					quotes.Add(keyInfo.KeyChar);
				}
			}

			// Redraw the line
			RenderLine(buffer.ToString(), 0);
			Console.SetCursorPosition(promptWidth + cursorIndex, Console.CursorTop);
		}

		if (quotes.Count > 0) {
			inString = quotes[0];
		} else {
			inString = null;
		}

		return buffer.ToString();
	}

	static void RenderLine(string text, int startPos) {
		Console.SetCursorPosition(startPos, Console.CursorTop);

		if (multilineActive) {
			Console.ForegroundColor = ConsoleColor.Black;
			Console.Write("... ");
		} else {
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write(">>> ");
		}

		Console.ResetColor();

		lexer.Reset(text);
		lexer.InStr = inString;
		var tokens = lexer.GetTokens();

		foreach ((int i, Token match) in tokens.WithIndex()) {
			string token = match.Value;

			if (match.InStr) {
				Console.ForegroundColor = ConsoleColor.DarkYellow; // Highlight InStrings
			} else if (keywords.Contains(token))
				Console.ForegroundColor = ConsoleColor.Magenta;
			else if (double.TryParse(token, out _))
				Console.ForegroundColor = ConsoleColor.Red; // Highlight numbers
			else if (constants.Contains(token))
				Console.ForegroundColor = ConsoleColor.Red; // Highlight constants
			else if (token.StartsWith("\"") || token.StartsWith("'") || token.StartsWith("`"))
				Console.ForegroundColor = ConsoleColor.DarkYellow; // Highlight strings
			else if (token == "self")
				Console.ForegroundColor = ConsoleColor.Red; // Highlight `self`
			else if (Parser.IsIdentifier(match) && IsPascalCase(token))
				Console.ForegroundColor = ConsoleColor.Blue; // Highlight classes/namespaces/enums
			else if (Parser.IsIdentifier(match) && i + 1 < tokens.Count && tokens[i + 1].Value == "(")
				Console.ForegroundColor = ConsoleColor.Cyan; // Highlight functions
			else
				Console.ForegroundColor = ConsoleColor.White;

			Console.Write(token);
		}

		// Crucial: If the user hit Backspace, the old last character might still be there.
		// We print a single space at the very end to "rub it out" and then move back.
		int currentPos = Console.CursorLeft;
		Console.Write(" ");
		Console.SetCursorPosition(currentPos, Console.CursorTop);

		Console.ResetColor();
	}

	public static bool IsPascalCase(string input) {
		if (string.IsNullOrEmpty(input)) return false;

		return Regex.IsMatch(input, @"[A-Z][A-Za-z_0-9]+");
	}

	public static bool IsIncomplete() {
		return parenCount > 0 || bracketCount > 0 || braceCount > 0
				|| quotes.Count > 0;
	}
}