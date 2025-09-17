using Cranberry.Errors;

namespace Cranberry.Namespaces;

public static class BuiltinNamespaces {
	public static void Init(Env env) {
		if (!env.Namespaces.TryAdd("Math", new N_Math()))
			throw new RuntimeError("Could not add namespace `Math`.");
	}
}