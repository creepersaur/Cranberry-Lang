using System.Globalization;
using System.Numerics;
using System.Text;

namespace Cranberry;

public class Token(string value, int line, int col, string filename, string filepath) {
	public readonly string Value = value;
	public readonly int Line = line;
	public readonly int Col = col;
	public readonly string FileName = filename;
	public readonly string FilePath = filepath;

	public override string ToString() => $"`{Value}` <{Line} : {Col}>";
	public static explicit operator string(Token t) => t.Value;
}

public class Lexer {
	private static readonly char[] PUNCTUATION = "\n!@$%^&*()[]{},./:;\\-=+~<>?".ToCharArray();
	public static readonly char[] QUOTES = "\"\'`".ToCharArray();
	private static readonly char[] SPACE = " \t\r".ToCharArray(); // NOTE: \n is in punctuation
	private static readonly string[] DOUBLE_PUNCS = "+= -= *= /= ++ -- // .. == != >= <= => ?? :: && ||".Split();

	private readonly char[] Text;
	private readonly string FileName;
	private readonly string FilePath;
	private int Pos;
	private int Line = 1;
	private int Col;
	private char? CurChar;

	public Lexer(string text, string filename, string filepath) {
		Text = text.ToCharArray();
		FileName = filename;
		FilePath = filepath;

		if (Text.Length > 0)
			CurChar = Text[0]; // Initialize current character at the start
	}

	public void Advance() {
		if (CurChar == '\n') {
			Line++;
			Col = -1;
		} else Col++;
		Pos += 1;
		if (Pos < Text.Length) {
			CurChar = Text[Pos];
		} else {
			CurChar = null; // CurChar should be null if there are no more characters
		}
	}

	private static bool IsPunctuation(char? c) => c.HasValue && PUNCTUATION.Contains(c.Value);
	private static bool IsQuote(char? c) => c.HasValue && QUOTES.Contains(c.Value);
	private static bool IsSpace(char? c) => c.HasValue && SPACE.Contains(c.Value);
	private static bool IsHexDigit(char c) => Uri.IsHexDigit(c);

	private static int HexValue(char c) {
		if (c is >= '0' and <= '9') return c - '0';
		if (c is >= 'a' and <= 'f') return 10 + (c - 'a');
		if (c is >= 'A' and <= 'F') return 10 + (c - 'A');
		throw new ArgumentException($"Not a hex digit: {c}");
	}

	private static string ProcessEscapeSequences(string str) {
		return str.Replace("\\n", "\n")
			.Replace("\\t", "\t")
			.Replace("\\#", "#")
			.Replace("\\r", "\r")
			.Replace(@"\\", "\\")
			.Replace("\\\"", "\"")
			.Replace("\\'", "'")
			.Replace("\\0", "\0");
	}

	public void Add(List<Token> tokens, string t) {
		tokens.Add(new Token(t, Line, Col - t.Length, FileName, FilePath));
	}

	public List<Token> GetTokens() {
		var tokens = new List<Token>();
		var curToken = "";
		char? instr = null;
		bool in_comment = false;
		bool escaped = false;
		Line = 1;
		Col = 0;

		while (CurChar.HasValue) {
			if (in_comment) {
				if (CurChar == '\n') {
					in_comment = false;
				}

				Advance();
				continue;
			}

			if (CurChar == '_' && int.TryParse(curToken, out _)) {
				Advance();
				continue;
			}

			if (CurChar == '0' && Pos + 1 < Text.Length && (Text[Pos + 1] == 'x' || Text[Pos + 1] == 'X')) {
				// only trigger when curToken is empty (so "10 0x1" or "(0x1)" work)
				if (curToken.Length < 1) {
					// Consume hex literal and normalize to decimal double string
					var hexLiteral = new StringBuilder();
					hexLiteral.Append('0'); // we'll keep the 0x in the internal buffer for parsing
					Advance(); // move from '0' to 'x'
					hexLiteral.Append(CurChar); // 'x' or 'X'
					Advance(); // move past 'x'

					// read hex integer part (allow underscores)
					var intPart = new StringBuilder();
					while (CurChar.HasValue && (IsHexDigit(CurChar.Value) || CurChar == '_')) {
						if (CurChar != '_') intPart.Append(CurChar);
						hexLiteral.Append(CurChar);
						Advance();
					}

					// possible fractional part
					string? fracPart = null;
					if (CurChar == '.') {
						hexLiteral.Append(CurChar);
						Advance(); // consume '.'
						var fracSb = new StringBuilder();
						while (CurChar.HasValue && (IsHexDigit(CurChar.Value) || CurChar == '_')) {
							if (CurChar != '_') fracSb.Append(CurChar);
							hexLiteral.Append(CurChar);
							Advance();
						}

						fracPart = fracSb.Length > 0 ? fracSb.ToString() : "";
					}

					// optional binary exponent 'p' or 'P' with decimal exponent
					int? exp = null;
					if (CurChar is 'p' or 'P') {
						Advance(); // consume 'p'
						var sign = 1;
						if (CurChar is '+' or '-') {
							if (CurChar == '-') sign = -1;
							Advance();
						}

						var expSb = new StringBuilder();
						while (CurChar.HasValue && char.IsDigit(CurChar.Value)) {
							expSb.Append(CurChar);
							Advance();
						}

						if (expSb.Length > 0) {
							exp = sign * int.Parse(expSb.ToString(), CultureInfo.InvariantCulture);
						}
					}

					// compute value
					double result;
					if (string.IsNullOrEmpty(fracPart)) {
						// integer hex (or no fractional digits)
						// parse maybe-large integer using BigInteger
						var hexDigits = intPart.Length > 0 ? intPart.ToString() : "0";
						try {
							var bigint = BigInteger.Parse(hexDigits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
							result = (double)bigint;
							if (exp.HasValue) result = result * Math.Pow(2, exp.Value);
						} catch {
							// fallback: incremental parse
							double accum = 0;
							foreach (var ch in hexDigits) {
								accum = accum * 16 + HexValue(ch);
							}

							result = accum;
							if (exp.HasValue) result = result * Math.Pow(2, exp.Value);
						}
					} else {
						// hex float: mantissa = intPart.fracPart in base 16, scale by 2^exp (if provided)
						double intVal = 0;
						var intDigits = intPart.Length > 0 ? intPart.ToString() : "0";
						foreach (var ch in intDigits) {
							intVal = intVal * 16 + HexValue(ch);
						}

						double fracVal = 0;
						for (int i = 0; i < fracPart.Length; i++) {
							fracVal += HexValue(fracPart[i]) / Math.Pow(16, i + 1);
						}

						double mantissa = intVal + fracVal;
						int effectiveExp = exp ?? 0; // if no 'p' exponent given, treat exponent as 0
						result = mantissa * Math.Pow(2, effectiveExp);
					}

					// normalize to invariant-culture decimal string (double)
					var decString = result.ToString("R", CultureInfo.InvariantCulture);

					Add(tokens, decString);
					curToken = "";
					continue;
				}
			}


			// Handle escape sequences inside strings
			if (instr.HasValue && escaped) {
				curToken += CurChar;
				escaped = false;
				Advance();
				continue;
			}

			// Check for backslash (escape character) inside strings
			if (instr.HasValue && CurChar == '\\') {
				curToken += CurChar;
				escaped = true;
				Advance();
				continue;
			}

			if (IsSpace(CurChar) && !instr.HasValue) {
				if (curToken.Length > 0) {
					Add(tokens, curToken);
					curToken = "";
				}
			} else if (CurChar == '#' && !instr.HasValue) {
				if (curToken.Length > 0) {
					Add(tokens, curToken);
					curToken = "";
				}

				in_comment = true;
			} else if (IsPunctuation(CurChar) && !instr.HasValue) {
				if (curToken.Length > 0 && curToken[^1] is 'e' or 'E' && float.TryParse(curToken[..^1], out float _) && CurChar is '+' or '-') {
					curToken += CurChar;
					Advance();
					continue;
				}

				if (CurChar == '.' && int.TryParse(curToken, out int _) && Pos + 1 < Text.Length && int.TryParse(Text[Pos + 1].ToString(), out int _)) {
					curToken += CurChar;
				} else {
					if (curToken.Length > 0) {
						Add(tokens, curToken);
						curToken = "";
					}

					if (Pos + 1 < Text.Length && IsPunctuation(Text[Pos + 1])) {
						var double_punctuation = CurChar.Value + Text[Pos + 1].ToString();
						if (DOUBLE_PUNCS.Contains(double_punctuation)) {
							Add(tokens, CurChar.Value + Text[Pos + 1].ToString());
							Advance();
						} else {
							Add(tokens, CurChar.Value.ToString());
						}
					} else {
						Add(tokens, CurChar.Value.ToString());
					}
				}
			} else if (IsQuote(CurChar)) {
				if (instr.HasValue) {
					if (instr == CurChar) {
						instr = null;
						curToken += CurChar;

						Add(tokens, ProcessEscapeSequences(curToken));
						curToken = "";

						Advance();

						continue;
					}
				} else {
					if (curToken.Length > 0) {
						Add(tokens, curToken);
						curToken = "";
					}
					instr = CurChar;
				}

				curToken += CurChar;
			} else {
				curToken += CurChar;
			}

			Advance();
		}

		if (curToken.Length > 0)
			Add(tokens, curToken);

		return tokens;
	}

	public static void PrintTokens(List<Token> l) {
		Console.ForegroundColor = ConsoleColor.White;
		Console.WriteLine("[TOKENS]: {");

		foreach (var i in l) {
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write($"    {i}");
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(",");
		}

		Console.WriteLine("\n}");
	}
}