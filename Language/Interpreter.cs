using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry;

public class Interpreter : INodeVisitor<object> {
	public readonly Env env = new();

	public object Evaluate(Node node) {
		return node.Accept(this);
	}
	
	private bool IsTruthy(object? value) {
		return value switch {
			null => false,
			bool b => b,
			double d => d != 0.0,    // 0 is false, everything else true
			string s => !string.IsNullOrEmpty(s), // empty string is false
			_ => true  // everything else is true
		};
	}
	
	//////////////////////////////////////////
	// TYPES
	//////////////////////////////////////////
	
	public object VisitNumber(NumberNode node) => node.Value;
	public object? VisitNull(NullNode node) => null;
	public object VisitBool(BoolNode node) => node.Value;
	
	//////////////////////////////////////////
	// EXPRESSIONS
	//////////////////////////////////////////
	
	public object VisitVariable(VariableNode node) => env.Get(node.Name);

	public object VisitBinaryOp(BinaryOpNode node) {
		var left = Convert.ToDouble(Evaluate(node.Left));
		var right = Convert.ToDouble(Evaluate(node.Right));

		return node.Op switch {
			"+" => left + right,
			"-" => left - right,
			"/" => left / right,
			"*" => left * right,
			"^" => Math.Pow(left, right),
			"%" => left % right,
			
			_ => throw new RuntimeError($"Unknown binary expression: {node.Op}")
		};
	}

	public object VisitUnaryOp(UnaryOpNode node) {
		var u_value = Convert.ToDouble(Evaluate(node.Value));
		
		return node.Op switch {
			"-" => -u_value,
			"+" => u_value,
			
			_ => throw new RuntimeError($"Unknown unary expression: {node.Op}")
		};
	}
	
	//////////////////////////////////////////
	// STATEMENTS
	//////////////////////////////////////////

	public object? VisitLet(LetNode node) {
		var value = Evaluate(node.Value);
		env.Define(node.name, value);
		return null;
	}
	
	public object VisitAssignment(AssignmentNode node) {
		var value = Evaluate(node.Value);
		env.Set(node.Name, value);
		
		return value;
	}
	
	public object? VisitIF(IFNode node) {
		if (IsTruthy(Evaluate(node.Condition))) {
			foreach (var n in node.Then) {
				Evaluate(n);
			}
		}
		
		return null;
	}
}