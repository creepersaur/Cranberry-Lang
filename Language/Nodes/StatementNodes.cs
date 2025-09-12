namespace Cranberry.Nodes;

public class LetNode(string[] names, Node[] values) : Node {
	public readonly string[] Names = names;
	public readonly Node[] Values = values;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitLet(this)!;
	}
}

public class ScopeNode(Node[] statements) : Node {
	public readonly Node[] Statements = statements;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitScope(this)!;
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

public class FunctionDef(string name, string[] args, Node[] statements) : Node {
	public readonly string Name = name;
	public readonly string[] Args = args;
	public readonly Node[] Statements = statements;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitFunctionDef(this)!;
	}
}

public class ReturnNode(Node value) : Node {
	public readonly Node Value = value;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitReturn(this)!;
	}
}