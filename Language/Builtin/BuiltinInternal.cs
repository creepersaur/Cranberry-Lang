using System.Runtime.InteropServices;
using Cranberry.Errors;
using Cranberry.Nodes;
using Cranberry.Types;

// ReSharper disable LoopCanBeConvertedToQuery
namespace Cranberry.Builtin;

public static class BuiltinInternal {
	public static Node Print(object?[] args, bool new_line = false) {
		if (args.Length == 0) {
			if (new_line) Console.WriteLine("");
			else Console.Write("");

			return new NullNode();
		}

		string output = "";

		foreach (var t in args) {
			output += Misc.FormatValue(t!) + " ";
		}

		if (new_line) {
			Console.WriteLine(output[..^1]);
		} else {
			Console.Write(output[..^1]);
		}

		return new NullNode();
	}

	public static Node Error(Token start_token, object?[] args) {
		if (args.Length == 0) {
			Console.WriteLine("");

			return new NullNode();
		}

		string output = "";

		foreach (var t in args) {
			output += Misc.FormatValue(t!) + " ";
		}

		throw new ExecutionError(start_token, output[..^1]);
	}

	public static double ToNumber(object? arg) {
		try {
			if (arg is CString c)
				return Convert.ToDouble(c.Value);

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

	public static string? ToString(object? arg) {
		try {
			return Misc.FormatValue(arg!);
		} catch {
			// ignored
			throw new RuntimeError($"Cannot convert `{arg}` to a string");
		}
	}

	public static string Format(object?[] args) {
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

	public static CString Typeof(object?[] args) {
		if (args.Length > 1 && Misc.IsTruthy(args[1]))
			return new CString(args[0]!.GetType().ToString());

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
			CClass cl => cl.Name,
			CObject c => c.Class.Name,
			FunctionNode => "function",
			InternalFunction => "function",
			ObjectMethod => "function",
			CNamespace => "namespace",
			CList list => list.IsTuple ? "tuple" : "list",
			CDict => "dict",
			CStopwatch => "stopwatch",
			CFile => "file",
			CDirectory => "directory",

			_ => args[0]!.GetType().Name
		});
	}

	public static object AddrOf(object?[] args) {
		if (args.Length < 1)
			throw new RuntimeError("addrof() expects at least 1 argument (the data).");

		object? val = args[0];
		if (val is CList cl) val = cl.Items;
		if (val is CDict cd) val = cd.Items;
		if (val is CString cs) val = cs.Value;
		if (val is CClrObject co) val = co.Instance;
		if (val is CClrType ct) val = ct.ClrType;

		// 1. Determine the target C# primitive type
		// Check if the user actually provided a string for the second argument
		string? typeName = args.Length > 1 ? args[1]?.ToString()?.ToLower() : null;

		Type targetType;

		if (typeName != null) {
			// Explicit cast requested by user
			targetType = typeName switch {
				"float" => typeof(float),
				"double" => typeof(double),
				"uint" => typeof(uint),
				"int" => typeof(int),
				"byte" => typeof(byte),
				_ => throw new RuntimeError($"Unknown pointer cast type: {typeName}")
			};
		} else {
			// No type specified: Infer from the value
			// If it's a list/enumerable, check the first element. 
			// Since Cranberry uses doubles, this will usually default to double.
			targetType = val switch {
				IEnumerable<float> => typeof(float),
				IEnumerable<int> => typeof(int),
				IEnumerable<uint> => typeof(uint),
				float => typeof(float),
				int => typeof(int),
				uint => typeof(uint),
				_ => typeof(double) // The "Cranberry Default"
			};
		}

		Array container;

		// 2. Case A: The user passed a List/Array of numbers
		if (val is System.Collections.IEnumerable enumerable && val is not string) {
			var list = enumerable.Cast<object>().ToList();
			container = Array.CreateInstance(targetType, list.Count);
			for (int i = 0; i < list.Count; i++) {
				container.SetValue(Convert.ChangeType(list[i], targetType), i);
			}
		}
		// 3. Case B: The user passed a single number
		else {
			container = Array.CreateInstance(targetType, 1);
			container.SetValue(Convert.ChangeType(val, targetType), 0);
		}

		// 4. Return the CPointer wrapper
		// This pins the memory and handles the GCHandle internally
		return new CPointer(container);
	}
}