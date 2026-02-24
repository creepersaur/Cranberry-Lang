using Cranberry.Errors;

namespace Cranberry.Types;

public class CEnum(string name, Dictionary<string, object> members) : IMemberAccessible {
	public readonly string Name = name;
	public readonly Dictionary<string, object> Members = members;

	public object GetMember(object? member) {
		if (member is string m) {
			if (Members.TryGetValue(m, out var value)) return value;
		} else {
			throw new RuntimeError($"`Enum:{Name}` members can only be accessed using a string.");
		}
		
		throw new RuntimeError($"Tried to get unknown member: `{member}` on `Enum:{Name}`.");
	}

	public void SetMember(object? member, object? value) {
		throw new RuntimeError($"Can not set members on `Enum:{Name}`.");
	}

	public override string ToString() => $"Enum:{Name}";
}