namespace Cranberry.Nodes;

public class LetNode(string[] names, Node[] values) : Node {
	public readonly string[] Names = names;
	public readonly Node[] Values = values;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitLet(this)!;
	}
}

public class ScopeNode(BlockNode block) : Node {
	public readonly BlockNode Block = block;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitScope(this)!;
	}
}

public class IFNode(Node condition, BlockNode then, (Node, BlockNode)[] elif, BlockNode? else_statements) : Node {
	public readonly Node Condition = condition;
	public readonly BlockNode Then = then;
	public readonly (Node, BlockNode)[] Elif = elif;
	public readonly BlockNode? ElseStatements = else_statements;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitIF(this)!;
	}
}

public class FunctionDef(string name, string[] args, BlockNode block) : Node {
	public readonly string Name = name;
	public readonly string[] Args = args;
	public readonly BlockNode Block = block;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitFunctionDef(this)!;
	}
}

public class ReturnNode(Node? value) : Node {
	public readonly Node? Value = value;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitReturn(this)!;
	}
}

public class BreakNode(Node? value) : Node {
	public readonly Node? Value = value;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitBreak(this)!;
	}
}

public class ContinueNode : Node {
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitContinue(this)!;
	}
}

public class WhileNode(Node condition, BlockNode block) : Node {
	public readonly Node Condition = condition;
	public readonly Node Block = block;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitWhile(this)!;
	}
}

public class ForNode(string var_name, Node iterable, BlockNode block) : Node {
	public readonly string VarName = var_name;
	public readonly Node Iterable = iterable;
	public readonly BlockNode Block = block;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitFOR(this)!;
	}
}