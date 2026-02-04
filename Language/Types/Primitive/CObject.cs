using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CObject(CClass from_class) : IMemberAccessible {
	public readonly CClass Class = from_class;
	public readonly Dictionary<string, object?> Properties = new();

	public object? GetMember(object? member) {
		if (member is string m) {
			if (Properties.TryGetValue(m, out var value)) {
				if (value is FunctionNode f)
					return new ObjectMethod(this, f);
				return value;
			}
			if (Class.Functions.TryGetValue(m, out var node)) {
				return new ObjectMethod(this, node);
			}
		}
		
		// Console.WriteLine(Misc.FormatValue(Properties));
		
		throw new RuntimeError($"Tried to get unknown member: `{member}` on `Object:{Class.Name}`.");
	}

	public void SetMember(object? member, object? value) {
		if (member is CString c) {
			Properties[c.Value] = value;
			return;
		}

		if (member is string m) {
			Properties[m] = value;
			return;
		}

		throw new RuntimeError("Can only set string members on a ClassObject");
	}

	public override string ToString() {
		if (Class.Functions.TryGetValue("__tostring__", out var f)) {
			var value = Program.interpreter!.Evaluate(new FunctionCall(f.StartToken, "", [this]) {
				Target = f
			});

			if (value == null)
				throw new RuntimeError("`__tostring__()` expects a return value.");

			return value.ToString()!;
		}

		return $"Object:{Class.Name}";
	}
}

public class ObjectMethod(CObject target, FunctionNode func) {
	public readonly CObject Target = target;
	public readonly FunctionNode Func = func;
}