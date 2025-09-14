using System.Globalization;
using Range = Cranberry.Types.Range;

namespace Cranberry.Nodes;

/////////////////////////////////////////////////////////
// TYPES
/////////////////////////////////////////////////////////

public class NullNode : Node {
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitNull(this);
	}

	public override string ToString() => "nil";
}

public class NumberNode(double value) : Node {
	public readonly double Value = value;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitNumber(this);
	}

	public override string ToString() => Convert.ToString(Value, CultureInfo.InvariantCulture);
}

public class StringNode(string value) : Node {
	public readonly string Value = value;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitString(this);
	}

	public override string ToString() => Value;
}

public class BlockNode(Node[] statements) : Node {
	public readonly Node[] Statements = statements;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitBlock(this);
	}
}

public class BoolNode(bool value) : Node {
	public readonly bool Value = value;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitBool(this);
	}

	public override string ToString() => Convert.ToString(Value);
}

public class FunctionNode(string[] args, BlockNode block) : Node {
	public readonly string[] Args = args;
	public readonly BlockNode Block = block;
	public Dictionary<string, object?>? Env { get; set; }
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitFunction(this);
	}
}

public class RangeNode(Node start, Node end, Node step, bool inclusive) : Node {
	public readonly Node Start = start;
	public readonly Node End = end;
	public readonly Node Step = step;
	public readonly bool Inclusive = inclusive;
	
	public override Range Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitRange(this);
	}
}

public class ListNode(List<Node> items) : Node {
	public readonly List<Node> Items = items;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitList(this);
	}

	public override string ToString() {
		string s = "[";

		foreach (var (i, v) in Items.WithIndex()) {
			s += v;
			if (i != Items.Count - 1)
				s += ", ";
		}
		
		return s + "]";
	}
}

public class DictNode(Dictionary<Node, Node> items) : Node {
	public readonly Dictionary<Node, Node> Items = items;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitDict(this);
	}
}

/////////////////////////////////////////////////////////
// OPERATIONS
/////////////////////////////////////////////////////////

public class BinaryOpNode(Node left, string op, Node right) : Node {
	public readonly Node Left = left;
	public readonly string Op = op;
	public readonly Node Right = right;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitBinaryOp(this);
	}
}

public class UnaryOpNode(string op, Node value) : Node {
	public readonly Node Value = value;
	public readonly string Op = op;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitUnaryOp(this);
	}
	
	public override string ToString() => Op + Value;
}

public class VariableNode(string name) : Node {
	public readonly string Name = name;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitVariable(this);
	}
}

public class AssignmentNode(string name, Node value) : Node {
	public readonly string Name = name;
	public readonly Node Value = value;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitAssignment(this);
	}
}

public class ShorthandAssignmentNode(string name, string op, Node? value) : Node {
	public readonly string Name = name;
	public readonly string Op = op;
	public readonly Node? Value = value;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitShorthandAssignment(this);
	}
}

public class FunctionCall(string name, Node[] args) : Node {
	public readonly string Name = name;
	public readonly Node[] Args = args;
	public Node? Target { get; init; } // What we're calling (for lambdas)

	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitFunctionCall(this);
	}
}

public class MemberAccessNode(Node target, Node member) : Node {
	public readonly Node Target = target;
	public readonly Node Member = member;

	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitMemberAccess(this);
	}

	public override string ToString() => $"{Target}.{Member}";
}

public class FallbackNode(Node left, Node right) : Node {
	public readonly Node Left = left;
	public readonly Node Right = right;

	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitFallback(this);
	}
}