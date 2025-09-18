using Cranberry.Errors;
using Cranberry.Namespaces;
using Cranberry.Types;

namespace Cranberry;

public class Env {
	public Stack<Dictionary<string, object>> Variables;
	public readonly Dictionary<string, CNamespace> Namespaces = new();

	public Env() {
		Variables = new Stack<Dictionary<string, object>>();
		Variables.Push(new Dictionary<string, object>());
	}

	public bool Has(string name) {
		if (Namespaces.ContainsKey(name)) return true;

		foreach (var scope in Variables) {
			if (scope.ContainsKey(name))
				return true;
		}

		return false;
	}
	
	public bool HasNamespace(string name) {
		return Namespaces.ContainsKey(name);
	}

	public void Push(Dictionary<string, object?>? vars = null) {
		Variables.Push((vars ?? new Dictionary<string, object>()!)!);
	}

	public void Pop() {
		Variables.Pop();
	}
	
	public object Get(string name) {
		if (Namespaces.TryGetValue(name, out var o))
			return o;
		
		foreach (var scope in Variables) {
			if (scope.TryGetValue(name, out object? value))
				return value;
		}

		throw new RuntimeError($"Undefined variable `{name}`.");
	}
	
	public CNamespace GetNamespace(string name) {
		if (Namespaces.TryGetValue(name, out var o))
			return o;
		
		throw new RuntimeError($"Namespace `{name}` doesn't exist in env.");
	}

	public void Set(string name, object value) {
		foreach (var scope in Variables) {
			if (scope.ContainsKey(name)) {
				scope[name] = value;
				return;
			}
		}
		
		throw new RuntimeError($"Tried to set unknown variable: `{name}`");
	}
	
	public void Define(string name, object value) {
		Variables.Peek()[name] = value;
	}
	
	public void DefineNamespace(CNamespace value) {
		if (!Namespaces.TryAdd(value.Name, value))
			throw new RuntimeError($"Cannot define namespace with duplicate name `{value.Name}`");
	}
}