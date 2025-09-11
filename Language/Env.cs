using Cranberry.Errors;

namespace Cranberry;

public class Env {
	public readonly Stack<Dictionary<string, object?>> Variables;

	public Env() {
		Variables = new Stack<Dictionary<string, object?>>();
		Variables.Push(new Dictionary<string, object?>());
	}
	
	public object Get(string name) {
		foreach (var scope in Variables) {
			if (scope.TryGetValue(name, out object? value))
				return value!;
		}

		throw new RuntimeError($"Undefined variable `{name}`.");
	}

	public void Set(string name, object? value) {
		if (Variables.Peek().ContainsKey(name)) {
			Variables.Peek()[name] = value;
		} else
			throw new RuntimeError($"Tried to set unknown variable: `{name}`");
	}
	
	public void Define(string name, object? value) {
		Variables.Peek()[name] = value;
	}
}