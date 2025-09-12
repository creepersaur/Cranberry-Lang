namespace Cranberry.Nodes;

/////////////////////////////////////////////////////////
// TYPES
/////////////////////////////////////////////////////////

public class NullNode : Node {
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitNull(this)!;
	}
}

public class NumberNode(double value) : Node {
	public readonly double Value = value;
    
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitNumber(this);
	}
}

public class StringNode(string value) : Node {
	public readonly string Value = value;
    
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitString(this);
	}
}

public class BoolNode(bool value) : Node {
	public readonly bool Value = value;
    
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitBool(this);
	}
}

public class FunctionNode(string[] args, Node[] statements) : Node {
	public readonly string[] Args = args;
	public readonly Node[] Statements = statements;
	
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitFunction(this);
	}
}

/////////////////////////////////////////////////////////
// OPERATIONS
/////////////////////////////////////////////////////////

public class BinaryOpNode(Node left, string op, Node right) : Node {
	public readonly Node Left = left;
	public readonly string Op = op;
	public readonly Node Right = right;
    
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitBinaryOp(this);
	}
}

public class UnaryOpNode(string op, Node value) : Node {
	public readonly Node Value = value;
	public readonly string Op = op;
    
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitUnaryOp(this);
	}
}

public class VariableNode(string name) : Node {
	public readonly string Name = name;
    
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitVariable(this);
	}
}

public class AssignmentNode(string name, Node value) : Node {
	public readonly string Name = name;
	public readonly Node Value = value;
    
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitAssignment(this);
	}
}

public class ShorthandAssignmentNode(string name, string op, Node? value) : Node {
	public readonly string Name = name;
	public readonly string Op = op;
	public readonly Node? Value = value;
    
	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitShorthandAssignment(this);
	}
}

public class FunctionCall(string name, Node[] args) : Node {
	public readonly string Name = name;
	public readonly Node[] Args = args;
	public Node? Target { get; set; } // What we're calling (for lambdas)

	public override T Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitFunctionCall(this)!;
	}
}