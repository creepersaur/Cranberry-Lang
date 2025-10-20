using Cranberry.Nodes;

namespace Cranberry.Builtin;

public class InternalFunction(Func<Token?, object?[], object?> func) : Node(null) {
	public object? Call(object[] args, Token? start_token = null) => func(start_token, args);
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitInternalFunction(this);
	}
	
	public override string ToString() => "<internal function>";
}