using Cranberry.Errors;
using Cranberry.Nodes;
using Cranberry.Types;

// ReSharper disable LoopCanBeConvertedToQuery
namespace Cranberry.Builtin;

public static class BuiltinFunctions {
	public static Node Print(List<object> args, bool new_line = false) {
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
			try {
				return Convert.ToDouble(Convert.ToInt32(arg));
			} catch {
				// ignored
				throw new RuntimeError($"Cannot convert `{arg}` to a number");
			}
		}
	}

	public static string? ToString(object arg) {
		try {
			return Misc.FormatValue(arg);
		} catch {
			// ignored
			throw new RuntimeError($"Cannot convert `{arg}` to a number");
		}
	}

	public static string Format(List<object> args) {
		var enumerator = args.GetEnumerator();
		enumerator.MoveNext();

		string template;
		if (enumerator.Current is CString c) {
			template = c.Value;
		} else if (enumerator.Current is string t) {
			template = t;
		} else {
			throw new RuntimeError("First argument of `format()` must be the template string.");
		}

		while (enumerator.MoveNext()) {
			var id = template.IndexOf("{}", StringComparison.Ordinal);
			if (id < 0) {
				throw new RuntimeError("FormatException: Input string was not in a correct format (or expected less arguments).");
			}
			template = template[..id] + Misc.FormatValue(enumerator.Current) + template[(id + 2)..];
		}

		return template;
	}

	public static CString Typeof(List<object> args) {
		return new CString(args[0] switch {
			CString => "string",
			double => "number",
			int => "number",
			long => "number",
			byte => "number",
			sbyte => "number",
			char => "char",
			bool => "bool",
			null => "null",
			NullNode => "null",
			CClass => "class",
			CObject c => c.Class.Name,
			FunctionNode => "function",
			InternalFunction => "function",
			ObjectMethod => "function",
			CNamespace => "namespace",
			CList => "list",
			CDict => "dict",
			CStopwatch => "stopwatch",
			CFile => "file",
			CDirectory => "directory",
			
			_ => args[0].GetType().Name
		});
	}
}