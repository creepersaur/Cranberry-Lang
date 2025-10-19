using System.Globalization;
using Cranberry.Errors;
using Cranberry.Nodes;
using Cranberry.Types;

namespace Cranberry.Builtin;

public abstract class Misc {
	public static string? FormatValue(object v, bool show_quotes = false, bool simple_classes = false, HashSet<object?>? seen = null, bool tuple = false) {
		switch (v) {
			case null:
				return "null";
			case CString s:
				return show_quotes switch {
					true => "\"" + s.Value + "\"",
					false => s.Value
				};
			case string s:
				return show_quotes switch {
					true => "\"" + s + "\"",
					false => s
				};
			case double d:
				return d.ToString(CultureInfo.InvariantCulture);
			case bool b:
				return b ? "true" : "false";
			case CList clist:
				if (clist.IsTuple) return FormatValue(clist.Items, tuple: true);
				return FormatValue(clist.Items);
			case CDict cdict:
				return FormatValue(cdict.Items);
		}

		// protect from cycles
		if (!simple_classes) {
			seen ??= [];
			switch (v) {
				case Dictionary<object, object> _ when !seen.Add(v):
					return "{...}"; // cycle guard
				case Dictionary<object, object> id: {
					var parts = new List<string>();
					foreach (var item in id) parts.Add($"{FormatValue(item.Key, true, false, seen)} : {FormatValue(item.Value, true, false, seen)}");
					return "{" + string.Join(", ", parts) + "}";
				}
				case System.Collections.IEnumerable ie and not string: {
					if (!seen.Add(v)) return "[cyclic...]"; // cycle guard

					var parts = new List<string?>();
					foreach (var item in ie) parts.Add(FormatValue(item, true, false, seen));
					if (tuple)
						return "(" + string.Join(", ", parts) + ")";
					
					return "[" + string.Join(", ", parts) + "]";
				}
			}
		} else {
			seen ??= [];
			switch (v) {
				case Dictionary<object, object> _ when !seen.Add(v):
					return "{...}"; // cycle guard
				case Dictionary<object, object> id: {
					if (id.Count < 1) {
						return "dict<Empty>";
					}

					var keys_type = id.Keys.First().GetType();
					var values_type = id.Values.First().GetType();

					string kname = keys_type.Name;
					string kvalue = values_type.Name;

					foreach (var (key, value) in id) {
						if (key.GetType() != keys_type) {
							kname = "any";
						}

						if (value.GetType() != values_type) {
							kvalue = "any";
						}
					}

					return $"dict<{kname}, {kvalue}>";
				}
			}
		}

		return v.ToString() ?? "null";
	}

	public static int DoubleToIndex(object _d, int length, bool allowNegative = false, double tol = 1e-9) {
		double d = Convert.ToDouble(_d);
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

	public static bool IsNumber(object value) {
		return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
	}

	public static bool TryGetInt(object? o, out int v) {
		v = 0;
		switch (o) {
			case int i:
				v = i;
				return true;
			case long l and >= int.MinValue and <= int.MaxValue:
				v = (int)l;
				return true;
			case double d when d % 1 == 0 && d is >= int.MinValue and <= int.MaxValue:
				v = (int)d;
				return true;
			default:
				return int.TryParse(o?.ToString(), out v);
		}
	}
	
	public static bool IsTruthy(object? value) {
		return value switch {
			NullNode => false,
			null => false,
			bool b => b,
			double d => d != 0.0, // 0 is false, everything else true
			_ => true // everything else is true
		};
	}
}