namespace Cranberry.Nodes;

public class LetNode(string name, Node value) : Node {
	public readonly string name = name;
	public readonly Node Value = value;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitLet(this)!;
	}
}

public class IFNode(Node condition, Node[] then) : Node {
	public readonly Node Condition = condition;
	public readonly Node[] Then = then;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitIF(this)!;
	}
}