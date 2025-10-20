namespace Cranberry.Builtin;

public abstract class FuncGen {
	public static Dictionary<string, InternalFunction> GenerateFunctions((string, InternalFunction)?[] functions) {
		var funcs = new Dictionary<string, InternalFunction>();

		foreach (var x in functions) {
			if (x is (not null, not null) f) {
				funcs[f.Item1] = f.Item2;
			}
		}
	
		return funcs;
	}
	
	public static (string, InternalFunction) FuncInternal(string? name, Func<Token?, object?[], object?> implementation) {
		return (name, new InternalFunction(implementation))!;
	}
}