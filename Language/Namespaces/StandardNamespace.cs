using Cranberry.Errors;
using Cranberry.Types;

namespace Cranberry.Namespaces;

public class StandardNamespace(Interpreter interpreter) : CNamespace("Std", true) {
	private readonly Interpreter Interpreter = interpreter;
	private readonly string[] Spaces = ["Math", "IO", "Task"];

	public CNamespace Register(string space) {
		if (space == "Math") {
			var x = new N_Math();
			env.Namespaces.TryAdd("Math", x);
			return x;
		}

		if (space == "IO") {
			var x = new N_IO();
			env.Namespaces.TryAdd("IO", x);
			return x;
		}

		if (space == "Task") {
			var x = new N_Task(Interpreter);
			env.Namespaces.TryAdd("Task", x);
			return x;
		}

		throw new RuntimeError($"Tried registering unknown Std Namespace: `{space}`");
	}

	public override object GetMember(object? member) {
		if (member is string m) {
			if (env.Namespaces.TryGetValue(m, out var value))
				return value;
			
			if (Spaces.Contains(m)) {
				return Register(m);
			}

			if (env.Has(m)) return env.Get(m);

			throw new RuntimeError($"Namespace `{Name}` does not contain member `{m}`");
		}

		throw new RuntimeError($"Namespace `{Name}` only supports getting members using strings.");
	}
}