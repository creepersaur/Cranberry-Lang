namespace Cranberry;

public class Lexer {
	private static readonly char[] PUNCTUATION = "!@#$%^&*()[]{},./:;\\-=+`~<>?".ToCharArray();
	private static readonly char[] QUOTES = "\"\'".ToCharArray();
	private static readonly char[] SPACE = " \n\t\r".ToCharArray();

	private readonly char[] Text;
	private int Pos;
	private char? CurChar;

	public Lexer(string text) {
		Text = text.ToCharArray();

		if (Text.Length > 0)
			CurChar = Text[0]; // Initialize current character at the start
	}

	public void Advance() {
		Pos += 1;
		if (Pos < Text.Length) {
			CurChar = Text[Pos];
		} else {
			CurChar = null; // CurChar should be null if there are no more characters
		}
	}

	bool IsPunctuation(char? c) => c.HasValue && PUNCTUATION.Contains(c.Value);
	bool IsQuote(char? c) => c.HasValue && QUOTES.Contains(c.Value);

	bool IsSpace(char? c) => c.HasValue && SPACE.Contains(c.Value);

	public List<string> GetTokens() {
		var tokens = new List<string>();
		var curToken = "";
		char? instr = null;

		while (CurChar.HasValue) {
			if (IsSpace(CurChar) && !instr.HasValue) {
				if (curToken.Length > 0) {
					tokens.Add(curToken);
					curToken = "";
				}
			} else if (IsPunctuation(CurChar) && !instr.HasValue) {
				if (CurChar == '.' && int.TryParse(curToken, out int _)) {
					curToken += CurChar;
				} else {
					if (curToken.Length > 0) {
						tokens.Add(curToken);
						curToken = "";
					}
					
					if (Pos + 1 < Text.Length && IsPunctuation(Text[Pos + 1])) {
						tokens.Add(CurChar.Value.ToString() + Text[Pos + 1].ToString());
						Advance();
					} else {
						tokens.Add(CurChar.Value.ToString());
					}
				}
			} else if (IsQuote(CurChar)) {
				if (instr.HasValue) {
					if (instr == CurChar) {
						instr = null;
						curToken += CurChar;

						tokens.Add(curToken);
						curToken = "";

						Advance();

						continue;
					}
				} else {
					instr = CurChar;
				}

				curToken += CurChar;
			} else {
				curToken += CurChar;
			}

			Advance();
		}

		if (curToken.Length > 0)
			tokens.Add(curToken);

		return tokens;
	}

	public static void PrintTokens<T>(List<T> l) {
		Console.ForegroundColor = ConsoleColor.White;
		Console.WriteLine("[TOKENS]: {");

		foreach (var i in l) {
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write($"    {i}");
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(",");
		}

		Console.WriteLine("\n}");
	}
}