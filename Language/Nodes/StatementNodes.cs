namespace Cranberry.Nodes;

public class LetNode(string[] names, Node[] values, bool constant) : Node {
	public readonly string[] Names = names;
	public readonly Node[] Values = values;
	public readonly bool Constant = constant;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitLet(this);
	}
}

public class ScopeNode(BlockNode block) : Node {
	public readonly BlockNode Block = block;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitScope(this);
	}
}

public class IFNode(Node condition, BlockNode then, (Node, BlockNode)[] elif, BlockNode? else_statements) : Node {
	public readonly Node Condition = condition;
	public readonly BlockNode Then = then;
	public readonly (Node, BlockNode)[] Elif = elif;
	public readonly BlockNode? ElseStatements = else_statements;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitIF(this);
	}
}

public class FunctionDef(string name, string[] args, BlockNode? block) : Node {
	public readonly string Name = name;
	public readonly string[] Args = args;
	public readonly BlockNode? Block = block;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitFunctionDef(this);
	}
}

public class ClassDef(string name, FunctionDef[] funcs, FunctionNode? constructor) : Node {
	public readonly string Name = name;
	public readonly FunctionDef[] Functions = funcs;
	public readonly FunctionNode? Constructor = constructor;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitClassDef(this);
	}
}

public class ReturnNode(Node? value) : Node {
	public readonly Node? Value = value;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitReturn(this);
	}
}

public class BreakNode(Node? value) : Node {
	public readonly Node? Value = value;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitBreak(this);
	}
}

public class OutNode(Node? value) : Node {
	public readonly Node? Value = value;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitOut(this);
	}
}

public class ContinueNode : Node {
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitContinue(this);
	}
}

public class WhileNode(Node condition, BlockNode block) : Node {
	public readonly Node Condition = condition;
	public readonly Node Block = block;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitWhile(this);
	}
}

public class ForNode(string var_name, Node iterable, BlockNode block) : Node {
	public readonly string VarName = var_name;
	public readonly Node Iterable = iterable;
	public readonly BlockNode Block = block;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitFOR(this);
	}
}

public class SwitchNode(Node expr, (Node[], BlockNode)[] cases, BlockNode? default_case) : Node {
	public readonly Node Expr = expr;
	public readonly (Node[], BlockNode)[] Cases = cases;
	public readonly BlockNode? DefaultCase = default_case;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitSwitch(this);
	}
}

public class UsingDirective(object[] names, Dictionary<string, string> aliases, List<string> wildcards) : Node {
	public readonly object[] Names = names;
	public readonly Dictionary<string, string> Aliases = aliases;
	public readonly List<string> Wildcards = wildcards;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitUsingDirective(this);
	}
}

public class NamespaceDirective(object[] names, BlockNode? block = null) : Node {
	public readonly object[] Names = names;
	public readonly BlockNode? Block = block;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitNamespaceDirective(this);
	}
}

public class IncludeDirective(Node paths) : Node {
	public readonly Node Paths = paths;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitIncludeDirective(this);
	}
}

public class DecoratorNode(string name, Node[] args, FunctionDef? func = null) : Node {
	public readonly string Name = name;
	public readonly Node[] Args = args;
	public readonly FunctionDef? Func = func;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitDecorator(this);
	}
}