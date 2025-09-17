namespace Cranberry.Builtin;

public class FuncGen {
	public static Dictionary<string, InternalFunction> GenerateFunctions((string, InternalFunction)[] functions) {
		var funcs = new Dictionary<string, InternalFunction>();

		foreach (var (i, v) in functions) {
			funcs[i] = v;
		}
	
		return funcs;
	}
	
	public static (string, InternalFunction) FuncInternal(string name, Func<object?[], object?> implementation) {
		return (name, new InternalFunction(implementation));
	}
}