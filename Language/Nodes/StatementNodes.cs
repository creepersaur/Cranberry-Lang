namespace Cranberry.Nodes;

public class LetNode(string name, Node value) : Node {
	public readonly string name = name;
	public readonly Node Value = value;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitLet(this)!;
	}
}

public class IFNode(Node condition, Node[] then, (Node, Node[])[] elif, Node[] else_statements) : Node {
	public readonly Node Condition = condition;
	public readonly Node[] Then = then;
	public readonly (Node, Node[])[] Elif = elif;
	public readonly Node[] ElseStatements = else_statements;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitIF(this)!;
	}
}