using System.Text.RegularExpressions;

namespace Cranberry.Errors;

public abstract partial class ErrorPrinter {
	public static int? PrintError(Exception e, string message) {
		Console.Write("[");
		Console.ForegroundColor = ConsoleColor.Red;
		Console.Write(message);
		Console.ResetColor();
		Console.Write("]: ");
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine(e.Message);
		Console.ResetColor();
		
		return null;
	}

	public static int? PrintErrorLine(Token start_token, ConsoleColor? color, bool full_line = false) {
		Console.ForegroundColor = ConsoleColor.Blue;
		Console.Write(" --> ");
		Console.ResetColor();
		var path = Path.GetRelativePath(System.Environment.CurrentDirectory, start_token.FilePath);
		Console.WriteLine($"{path}:{start_token.Line}:{int.Max(start_token.Col, 1)}");

		string line = File.ReadLines(start_token.FilePath).ToArray()[start_token.Line - 1];
		if (line.Length > 0 && line[0] == '\uFEFF')
			line = line[1..];

		string start_indent = line[0..int.Max(start_token.Col, 0)];
		start_indent = start_indent.TrimStart();
		start_indent = MyRegex().Replace(start_indent, " ");

		line = line.TrimStart();
		
		var line_number = $"{start_token.Line} |    ";
		Console.ForegroundColor = color ?? ConsoleColor.Blue;
		if (start_token.Line > 1) {
			Console.WriteLine($"  {start_token.Line - 1} |    ");
		} else {
			Console.WriteLine();
		}
		Console.Write($"  {line_number}");
		Console.ResetColor();
		Console.WriteLine($"{line}");

		if (start_token.Line > 1) {
			line_number = "";
			Console.ForegroundColor = color ?? ConsoleColor.Blue;
			Console.Write($"  {start_token.Line + 1} |    ");
		} else {
			Console.Write("  ");
		}

		Console.ForegroundColor = color ?? ConsoleColor.Red;
		if (full_line) {
			Console.WriteLine($"{new string(' ', line_number.Length)}{new string('^', line.TrimEnd().Length)}");
		} else {
			Console.WriteLine($"{new string(' ', line_number.Length)}{start_indent}{new string('^', start_token.Value.Length)}");
		}
		if (start_token.Line < 2) {
			Console.WriteLine(":");
		}
		Console.ResetColor();
		return null;
	}

    [GeneratedRegex(@"[^\t]")]
    private static partial Regex MyRegex();
}