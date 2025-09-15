using System.Globalization;
using Cranberry.Errors;

namespace Cranberry.Builtin;

public abstract class Misc {
	public static string FormatValue(object? v, bool show_quotes = false, HashSet<object>? seen = null) {
		switch (v) {
			case null:
				return "null";
			case string s:
				return show_quotes switch {
					true => "\"" + s + "\"",
					false => s
				};
			case double d:
				return d.ToString(CultureInfo.InvariantCulture);
			case bool b:
				return b ? "true" : "false";
		}

		// protect from cycles
		seen ??= [];
		switch (v) {
			case Dictionary<object, object> _ when !seen.Add(v):
				return "{...}"; // cycle guard
			case Dictionary<object, object> id: {
				var parts = new List<string>();
				foreach (var item in id) parts.Add($"{FormatValue(item.Key, true, seen)} : {FormatValue(item.Value, true, seen)}");
				return "{" + string.Join(", ", parts) + "}";
			}
			case System.Collections.IEnumerable ie and not string: {
				if (!seen.Add(v)) return "[...]"; // cycle guard

				var parts = new List<string>();
				foreach (var item in ie) parts.Add(FormatValue(item, true, seen));
				return "[" + string.Join(", ", parts) + "]";
			}
			default:
				return v.ToString() ?? "null";
		}
	}
	
	public static int DoubleToIndex(double d, int length, bool allowNegative = false, double tol = 1e-9) {
		if (double.IsNaN(d)) throw new RuntimeError("Index is NaN");
		if (double.IsInfinity(d)) throw new RuntimeError("Index is infinite");

		// Accept only integer-valued doubles. Use a small tolerance for floating noise.
		double truncated = Math.Truncate(d);
		if (Math.Abs(d - truncated) > tol) {
			throw new RuntimeError($"Index {d} is not an integer");
		}

		// Convert to long first to check range, then to int
		long asLong = (long)truncated; // safe if truncated in double-exact integer range
		if (asLong is < int.MinValue or > int.MaxValue)
			throw new RuntimeError($"Index {d} is outside integer range");

		int idx = (int)asLong;

		// Normalize negative indices if allowed (Python style: -1 -> last element)
		if (allowNegative && idx < 0) idx = length + idx;

		// Bounds check
		if (idx < 0 || idx >= length) throw new RuntimeError($"Index {d} out of range (0..{length - 1})");

		return idx;
	}
}