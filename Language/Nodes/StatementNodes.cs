namespace Cranberry.Nodes;

public class LetNode(Token start_token, object[] names, Node[] values, bool constant) : Node(start_token) {
	public readonly object[] Names = names;
	public readonly Node[] Values = values;
	public readonly bool Constant = constant;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitLet(this, false);
	}
}

public class ScopeNode(Token start_token, BlockNode block) : Node(start_token) {
	public readonly BlockNode Block = block;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitScope(this);
	}
}

public class IFNode(Token start_token, Node condition, BlockNode then, (Node, BlockNode)[] elif, BlockNode? else_statements) : Node(start_token) {
	public readonly Node Condition = condition;
	public readonly BlockNode Then = then;
	public readonly (Node, BlockNode)[] Elif = elif;
	public readonly BlockNode? ElseStatements = else_statements;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitIF(this);
	}
}

public class FunctionDef(Token start_token, string name, string[] args, BlockNode? block) : Node(start_token) {
	public readonly string Name = name;
	public readonly string[] Args = args;
	public readonly BlockNode? Block = block;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitFunctionDef(this);
	}
}

public class ClassDef(Token start_token, string name, FunctionDef[] funcs, LetNode[] lets, FunctionNode? constructor) : Node(start_token) {
	public readonly string Name = name;
	public readonly FunctionDef[] Functions = funcs;
	public readonly LetNode[] Lets = lets;
	public readonly FunctionNode? Constructor = constructor;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitClassDef(this);
	}
}

public class ReturnNode(Token start_token, Node? value) : Node(start_token) {
	public readonly Node? Value = value;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitReturn(this);
	}
}

public class BreakNode(Token start_token, Node? value) : Node(start_token) {
	public readonly Node? Value = value;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitBreak(this);
	}
}

public class OutNode(Token start_token, Node? value) : Node(start_token) {
	public readonly Node? Value = value;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitOut(this);
	}
}

public class ContinueNode(Token start_token) : Node(start_token) {
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitContinue(this);
	}
}

public class WhileNode(Token start_token, Node condition, BlockNode block) : Node(start_token) {
	public readonly Node Condition = condition;
	public readonly Node Block = block;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitWhile(this);
	}
}

public class ForNode(Token start_token, string var_name, Node iterable, BlockNode block) : Node(start_token) {
	public readonly string VarName = var_name;
	public readonly Node Iterable = iterable;
	public readonly BlockNode Block = block;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitFOR(this);
	}
}

public class SwitchNode(Token start_token, Node expr, (Node[], BlockNode)[] cases, BlockNode? default_case) : Node(start_token) {
	public readonly Node Expr = expr;
	public readonly (Node[], BlockNode)[] Cases = cases;
	public readonly BlockNode? DefaultCase = default_case;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitSwitch(this);
	}
}

public class UsingDirective(Token start_token, object[] names, Dictionary<string, string> aliases, List<string> wildcards) : Node(start_token) {
	public readonly object[] Names = names;
	public readonly Dictionary<string, string> Aliases = aliases;
	public readonly List<string> Wildcards = wildcards;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitUsingDirective(this);
	}
}

public class NamespaceDirective(Token start_token, object[] names, BlockNode? block = null) : Node(start_token) {
	public readonly object[] Names = names;
	public readonly BlockNode? Block = block;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitNamespaceDirective(this);
	}
}

public class DecoratorNode(Token start_token, string name, Node[] args, FunctionDef? func = null) : Node(start_token) {
	public readonly string Name = name;
	public readonly Node[] Args = args;
	public readonly FunctionDef? Func = func;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitDecorator(this);
	}
}