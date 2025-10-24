using System.Globalization;
using Cranberry.Types;

namespace Cranberry.Nodes;

/////////////////////////////////////////////////////////
// TYPES
/////////////////////////////////////////////////////////

public class NullNode(Token? start_token = null) : Node(start_token) {
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitNull(this);
	}

	public override string ToString() => "nil";
}

public class NumberNode(Token? start_token, double value) : Node(start_token) {
	public readonly double Value = value;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitNumber(this);
	}

	public override string ToString() => Convert.ToString(Value, CultureInfo.InvariantCulture);
}

public class StringNode(Token start_token, string value) : Node(start_token) {
	public readonly string Value = value;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitString(this);
	}

	public override string ToString() => Value;
}

public class BlockNode(Token start_token, Node[] statements) : Node(start_token) {
	public readonly Node[] Statements = statements;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitBlock(this);
	}
}

public class BoolNode(Token? start_token, bool value) : Node(start_token) {
	public readonly bool Value = value;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitBool(this);
	}

	public override string ToString() => Convert.ToString(Value);
}

public class FunctionNode(Token start_token, string[] args, BlockNode block) : Node(start_token) {
	public readonly string[] Args = args;
	public readonly BlockNode Block = block;
	public Dictionary<string, object>? Env { get; set; }
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitFunction(this);
	}
}

public class RangeNode(Token start_token, Node start, Node end, Node step, bool inclusive) : Node(start_token) {
	public readonly Node Start = start;
	public readonly Node End = end;
	public readonly Node Step = step;
	public readonly bool Inclusive = inclusive;
	
	public override CRange Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitRange(this);
	}
}

public class ListNode(Token start_token, List<Node> items, bool is_tuple = false) : Node(start_token) {
	public readonly List<Node> Items = items;
	public readonly bool IsTuple = is_tuple;
	
	public override object Accept<T>(INodeVisitor<T> visitor) {
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

public class DictNode(Token start_token, Dictionary<Node, Node> items) : Node(start_token) {
	public readonly Dictionary<Node, Node> Items = items;
	
	public override object Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitDict(this);
	}
}

/////////////////////////////////////////////////////////
// OPERATIONS
/////////////////////////////////////////////////////////

public class BinaryOpNode(Token start_token, Node left, string op, Node right) : Node(start_token) {
	public readonly Node Left = left;
	public readonly string Op = op;
	public readonly Node Right = right;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitBinaryOp(this);
	}
}

public class UnaryOpNode(Token start_token, string op, Node value) : Node(start_token) {
	public readonly Node Value = value;
	public readonly string Op = op;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitUnaryOp(this);
	}
	
	public override string ToString() => Op + Value;
}

public class VariableNode(Token start_token, string name) : Node(start_token) {
	public readonly string Name = name;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitVariable(this);
	}
}

public class AssignmentNode(Token start_token, string[] names, Node[] values) : Node(start_token) {
	public readonly string[] Names = names;
	public readonly Node[] Values = values;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitAssignment(this);
	}
}

public class ShorthandAssignmentNode(Token start_token, string name, string op, Node? value) : Node(start_token) {
	public readonly string Name = name;
	public readonly string Op = op;
	public readonly Node? Value = value;
    
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitShorthandAssignment(this);
	}
}

public class FunctionCall(Token? start_token, string name, object?[] args) : Node(start_token) {
	public readonly string Name = name;
	public readonly object?[] Args = args;
	public Node? Target { get; init; } // What we're calling (for lambdas)

	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitFunctionCall(this);
	}
}

public class MemberAccessNode(Token start_token, Node target, Node member) : Node(start_token) {
	public readonly Node Target = target;
	public readonly Node Member = member;

	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitMemberAccess(this);
	}

	public override string ToString() => $"{Target}.{Member}";
}

public class MemberAssignmentNode(Token start_token, Node target, Node member, object value) : Node(start_token) {
	public readonly Node Target = target;
	public readonly Node Member = member;
	public readonly object Value = value;

	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitMemberAssignment(this);
	}

	public override string ToString() => $"{Target}.{Member}";
}

public class MemberShorthandAssignmentNode(Token start_token, Node target, Node member, Node value, string op) : Node(start_token) {
	public readonly Node Target = target;
	public readonly Node Member = member;
	public readonly Node Value = value;
	public readonly string Op = op;

	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitMemberShorthandAssignment(this);
	}

	public override string ToString() => $"{Target} ()= {Member}";
}

public class FallbackNode(Token start_token, Node left, Node right) : Node(start_token) {
	public readonly Node Left = left;
	public readonly Node Right = right;

	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitFallback(this);
	}
}

public class CastNode(Token start_token, string type, object to_cast) : Node(start_token) {
	public readonly string Type = type;
	public readonly object ToCast = to_cast;

	public override object? Accept<T>(INodeVisitor<T> visitor) {
		return visitor.VisitCast(this);
	}
}

public class SignalNode(Token start_token, string name) : Node(start_token) {
	public readonly string Name = name;
	
	public override object? Accept<T>(INodeVisitor<T> visitor) {
		visitor.VisitSignal(this);
		return null;
	}
}