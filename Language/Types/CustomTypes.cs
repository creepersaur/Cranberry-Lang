using Cranberry.Errors;

namespace Cranberry.Types;

public abstract class CustomType;

public interface IMemberAccessible
{
	object GetMember(string name) {
		throw new RuntimeError($"Tried to get unknown member: `{name}`");
	}
	
	void SetMember(string name, object value) {
		throw new RuntimeError($"Cannot set member {name}.`");
	}
}