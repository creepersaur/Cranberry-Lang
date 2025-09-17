using Cranberry.Errors;

namespace Cranberry.Namespaces;

public static class BuiltinNamespaces {
	public static void Register(Interpreter interpreter, Env env, string space) {
		if (space == "Math")
			if (!env.Namespaces.TryAdd("Math", new N_Math()))
				throw new RuntimeError("Could not add namespace `Math`.");

		if (space == "IO")
			if (!env.Namespaces.TryAdd("IO", new N_IO()))
				throw new RuntimeError("Could not add namespace `IO`.");

		if (space == "Task")
			if (!env.Namespaces.TryAdd("Task", new N_Task(interpreter)))
				throw new RuntimeError("Could not add namespace `Task`.");
	}
}