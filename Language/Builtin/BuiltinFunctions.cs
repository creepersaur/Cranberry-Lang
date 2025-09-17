using Cranberry.Errors;
using Cranberry.Nodes;

// ReSharper disable LoopCanBeConvertedToQuery
namespace Cranberry.Builtin;

public static class BuiltinFunctions {
	public static Node Print(List<object?> args, bool new_line = false) {
		string output = "";

		foreach (var t in args) {
			output += Misc.FormatValue(t) + " ";
		}

		if (new_line) {
			Console.WriteLine(output[..^1]);
		} else {
			Console.Write(output[..^1]);
		}

		return new NullNode();
	}

	public static double ToNumber(object? arg) {
		try {
			return Convert.ToDouble(arg);
		} catch {
			// ignored
			throw new RuntimeError($"Cannot convert `{arg}` to a number");
		}
	}

	public static string ToString(object? arg) {
		try {
			return Misc.FormatValue(arg);
		} catch {
			// ignored
			throw new RuntimeError($"Cannot convert `{arg}` to a number");
		}
	}
}