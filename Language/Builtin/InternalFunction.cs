using Cranberry.Errors;
using Cranberry.Nodes;
using Cranberry.Types;

namespace Cranberry.Builtin;

public class InternalFunction(Func<Token?, object?[], object?> func, object? internalMethod = null) : Node(null), IMemberAccessible {
	// THE INTERNAL METHOD THAT CClrObject writes to.
	public object? InternalMethod = internalMethod ?? func;
	public object? Call(object[] args, Token? start_token = null) => func(start_token, args);
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitInternalFunction(this);
	}

	public object GetMember(object? member) {
		if (member is {} && member.ToString() == "_internal") {
			return InternalMethod!;
		}
		throw new RuntimeError($"Member ${member} not found on Internal Function.");
	}
	
	public override string ToString() => "<internal function>";
}