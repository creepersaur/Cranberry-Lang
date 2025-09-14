using System.Globalization;
using Cranberry.Errors;
using Cranberry.Nodes;

// ReSharper disable LoopCanBeConvertedToQuery
namespace Cranberry.Builtin;

public static class BuiltinFunctions {
	public static Node Print(object?[] args, bool new_line = false) {
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
}

class Misc {
	public static string FormatValue(object? v, HashSet<object>? seen = null) {
		if (v == null) return "null";
		if (v is string s) return "\"" + s + "\"";
		if (v is double d) return d.ToString(CultureInfo.InvariantCulture);
		if (v is bool b) return b ? "true" : "false";

		// protect from cycles
		seen ??= new HashSet<object>();
		if (v is Dictionary<object, object> id) {
			if (!seen.Add(v)) return "{...}"; // cycle guard

			var parts = new List<string>();
			foreach (var item in id) parts.Add($"{FormatValue(item.Key, seen)} : {FormatValue(item.Value, seen)}");
			return "{" + string.Join(", ", parts) + "}";
		}
		if (v is System.Collections.IEnumerable ie && !(v is string)) {
			if (!seen.Add(v)) return "[...]"; // cycle guard

			var parts = new List<string>();
			foreach (var item in ie) parts.Add(FormatValue(item, seen));
			return "[" + string.Join(", ", parts) + "]";
		}

		return v.ToString() ?? "null";
	}
}