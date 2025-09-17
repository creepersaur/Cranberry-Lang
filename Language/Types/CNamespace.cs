using Cranberry.Errors;

namespace Cranberry.Types;

public class CNamespace(string name, bool Constant = false) : IMemberAccessible {
	public readonly string Name = name;

	public override string ToString() => $"Namespace:{Name}";

	public Dictionary<string, object> Members = new();
	
	public object GetMember(object member) {
		if (member is string m) {
			if (Members.TryGetValue(m, out var value)) {
				return value;
			}

			throw new RuntimeError($"Namespace `{Name}` does not contain member `{m}`.");
		}

		throw new RuntimeError($"Namespace `{Name}` only supports getting members using strings.");
	}

	public void SetMember(object member, object value) {
		if (Constant)
			throw new RuntimeError($"Cannot set a member of namespace `{Name}`.");
		
		if (member is string m) {
			Members[m] = value;
		}

		throw new RuntimeError($"Namespace `{Name}` only supports setting members using strings.");
	}
}