using Cranberry.Errors;
using Cranberry.Namespaces;

namespace Cranberry;

public class Env {
	public readonly Stack<Dictionary<string, object?>> Variables;
	public readonly Dictionary<string, object> Namespaces = new();

	public Env() {
		Variables = new Stack<Dictionary<string, object?>>();
		Variables.Push(new Dictionary<string, object?>());
		
		BuiltinNamespaces.Init(this);
	}

	public void Push(Dictionary<string, object?>? vars = null) {
		Variables.Push(vars ?? new Dictionary<string, object?>());
	}

	public void Pop() {
		Variables.Pop();
	}
	
	public object? Get(string name) {
		if (Namespaces.TryGetValue(name, out var o))
			return o;
		
		foreach (var scope in Variables) {
			if (scope.TryGetValue(name, out object? value))
				return value;
		}

		throw new RuntimeError($"Undefined variable `{name}`.");
	}

	public void Set(string name, object? value) {
		foreach (var scope in Variables) {
			if (scope.ContainsKey(name)) {
				scope[name] = value;
				return;
			}
		}
		
		throw new RuntimeError($"Tried to set unknown variable: `{name}`");
	}
	
	public void Define(string name, object? value) {
		Variables.Peek()[name] = value;
	}
}