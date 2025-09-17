using Cranberry.Nodes;

namespace Cranberry.Builtin;

public class InternalFunction(Func<object?[], object?> _func) : Node {
	private readonly Func<object?[], object?> func = _func;

	public object? Call(params object[] args) => func(args);
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitInternalFunction(this);
	}
	
	public override string ToString() => "<internal function>";
}