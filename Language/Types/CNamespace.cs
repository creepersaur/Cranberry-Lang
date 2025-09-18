using Cranberry.Errors;

namespace Cranberry.Types;

public class CNamespace(string name, bool Constant = false) : IMemberAccessible {
	public readonly string Name = name;
	public readonly Env env = new();

	public override string ToString() => $"Namespace:{Name}";

	public virtual object GetMember(object? member) {
		if (member is string m) {
			if (env.Namespaces.TryGetValue(m, out var value))
				return value;

			if (env.Has(m)) return env.Get(m);

			throw new RuntimeError($"Namespace `{Name}` does not contain member `{m}`.");
		}

		throw new RuntimeError($"Namespace `{Name}` only supports getting members using strings.");
	}

	public void SetMember(object? member, object? value) {
		if (Constant)
			throw new RuntimeError($"Cannot set a member of namespace `{Name}`.");

		if (member is string m) env.Set(m, value!);

		throw new RuntimeError($"Namespace `{Name}` only supports setting members using strings.");
	}
}