using System.Text.RegularExpressions;

namespace Cranberry.Errors;

public abstract partial class ErrorPrinter {
	public static int? PrintError(Exception e, string message) {
		Console.Write("[");
		Console.ForegroundColor = ConsoleColor.Red;
		Console.Write(message);
		Console.ResetColor();
		Console.WriteLine("]: {0}", e.Message);
		
		return null;
	}

	public static int? PrintErrorLine(Token start_token, ConsoleColor? color, bool full_line = false) {
		string line = File.ReadLines(start_token.FilePath).ToArray()[start_token.Line - 1];
		if (line.Length > 0 && line[0] == '\uFEFF')
			line = line[1..];

		string start_indent = line[..int.Max(start_token.Col, 0)];
		start_indent = start_indent.TrimStart();
		start_indent = MyRegex().Replace(start_indent, " ");

		line = line.TrimStart();
		
		Console.WriteLine($"\n\t{line}");
		Console.ForegroundColor = color ?? ConsoleColor.Red;
		if (full_line) {
			Console.WriteLine($"\t{new string('^', line.TrimEnd().Length)}");
		} else {
			Console.WriteLine($"\t{start_indent}{new string('^', start_token.Value.Length)}");
		}
		Console.ResetColor();
		return null;
	}

    [GeneratedRegex(@"[^\t]")]
    private static partial Regex MyRegex();
}