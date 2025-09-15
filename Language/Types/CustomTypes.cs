using Cranberry.Errors;

namespace Cranberry.Types;

public interface IMemberAccessible
{
	object GetMember(object member) {
		throw new RuntimeError($"Tried to get unknown member: `{member}`");
	}
	
	void SetMember(string member, object value) {
		throw new RuntimeError($"Cannot set member {member}.`");
	}
}