using Cranberry.Errors;
using Cranberry.Types;

namespace Cranberry;

public class Env {
	public readonly Stack<Dictionary<string, object>> Variables;
	public readonly Stack<Dictionary<string, object>> Constants;
	public readonly Dictionary<string, CNamespace> Namespaces = new();

	public Env() {
		Variables = new Stack<Dictionary<string, object>>();
		Variables.Push(new Dictionary<string, object>());
		
		Constants = new Stack<Dictionary<string, object>>();
		Constants.Push(new Dictionary<string, object>());
	}

	public bool Has(string name) {
		if (Namespaces.ContainsKey(name)) return true;

		foreach (var scope in Variables) {
			if (scope.ContainsKey(name))
				return true;
		}
		
		foreach (var scope in Constants) {
			if (scope.ContainsKey(name))
				return true;
		}

		return false;
	}
	
	public bool HasNamespace(string name) {
		return Namespaces.ContainsKey(name);
	}

	public void Push(Dictionary<string, object>? vars = null) {
		Variables.Push(vars ?? new Dictionary<string, object>());
		Constants.Push(new Dictionary<string, object>());
	}

	public void Pop() {
		Variables.Pop();
		Constants.Pop();
	}
	
	public object Get(string name) {
		if (Namespaces.TryGetValue(name, out var o))
			return o;
		
		foreach (var scope in Variables) {
			if (scope.TryGetValue(name, out object? value))
				return value;
		}
		
		foreach (var scope in Constants) {
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
		
		foreach (var scope in Constants) {
			if (scope.ContainsKey(name)) {
				throw new RuntimeError($"Cannot set constant: `{name}`");
			}
		}

		throw new RuntimeError($"Tried to set unknown variable: `{name}`");
	}
	
	public void Define(string name, object value) {
		Variables.Peek()[name] = value;
	}
	
	public void DefineConstant(string name, object value) {
		Constants.Peek()[name] = value;
	}
	
	public void DefineNamespace(CNamespace value, string? alias = null) {
		Namespaces.TryAdd(alias ?? value.Name, value);
	}
	
	public void DefineWildcardNamespace(CNamespace value) {
		foreach (var pair in value.env.Constants.Peek()) {
			DefineConstant(pair.Key, pair.Value);
		}
		foreach (var pair in value.env.Variables.Peek()) {
			Define(pair.Key, pair.Value);
		}
		foreach (var pair in value.env.Namespaces) {
			DefineNamespace(pair.Value);
		}
	}
}