using Cranberry.Errors;
using Cranberry.Nodes;
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable DuplicatedStatements

namespace Cranberry;

public class Interpreter : INodeVisitor<object> {
	public readonly Env env = new();
	private const double TOLERANCE = 1e-9;

	public object Evaluate(Node node) {
		return node.Accept(this);
	}

	private static bool IsTruthy(object? value) {
		return value switch {
			null => false,
			bool b => b,
			double d => d != 0.0, // 0 is false, everything else true
			string s => !string.IsNullOrEmpty(s), // empty string is false
			_ => true // everything else is true
		};
	}

	//////////////////////////////////////////
	// TYPES
	//////////////////////////////////////////

	public object VisitNumber(NumberNode node) => node.Value;
	public object? VisitNull(NullNode node) => null;
	public object VisitBool(BoolNode node) => node.Value;
	public object VisitString(StringNode node) => node.Value;
	public object VisitFunction(FunctionNode node) => node;

	//////////////////////////////////////////
	// EXPRESSIONS
	//////////////////////////////////////////

	public object VisitVariable(VariableNode node) => env.Get(node.Name);

	public object VisitBinaryOp(BinaryOpNode node) {
		object leftVal = Evaluate(node.Left);
		object rightVal = Evaluate(node.Right);

		return node.Op switch {
			// Addition - handle string concatenation
			"+" => HandleAddition(leftVal, rightVal),

			// Arithmetic (numbers only)
			"-" => Convert.ToDouble(leftVal) - Convert.ToDouble(rightVal),
			"/" => Convert.ToDouble(leftVal) / Convert.ToDouble(rightVal),
			"*" => HandleMultiplication(leftVal, rightVal),
			"^" => Math.Pow(Convert.ToDouble(leftVal), Convert.ToDouble(rightVal)),
			"%" => Convert.ToDouble(leftVal) % Convert.ToDouble(rightVal),
			"//" => Math.Floor(Convert.ToDouble(leftVal) / Convert.ToDouble(rightVal)),

			// Comparisons - handle different types
			"==" => AreEqual(leftVal, rightVal),
			"!=" => !AreEqual(leftVal, rightVal),
			"<" => CompareValues(leftVal, rightVal) < 0,
			">" => CompareValues(leftVal, rightVal) > 0,
			"<=" => CompareValues(leftVal, rightVal) <= 0,
			">=" => CompareValues(leftVal, rightVal) >= 0,

			_ => throw new RuntimeError($"Unknown binary expression: {node.Op}")
		};
	}

	private object HandleAddition(object? left, object? right) {
		// String concatenation
		if (left is string || right is string) {
			return $"{left}{right}";
		}

		// Number addition
		return Convert.ToDouble(left) + Convert.ToDouble(right);
	}
	
	private static object HandleMultiplication(object? left, object? right) {
		// String multiplication
		if (left is string && right is double) {
			return string.Concat(Enumerable.Repeat(left, Convert.ToInt32(right)));
		}

		// Number addition
		return Convert.ToDouble(left) * Convert.ToDouble(right);
	}

	private bool AreEqual(object? left, object? right) {
		if (left == null && right == null) return true;
		if (left == null || right == null) return false;

		// Float comparison with tolerance
		if (left is double leftDouble && right is double rightDouble) {
			return Math.Abs(leftDouble - rightDouble) < TOLERANCE;
		}

		return left.Equals(right);
	}

	private int CompareValues(object? left, object? right) {
		// String comparison
		if (left is string leftStr && right is string rightStr) {
			return string.CompareOrdinal(leftStr, rightStr);
		}

		// Number comparison
		double leftNum = Convert.ToDouble(left);
		double rightNum = Convert.ToDouble(right);
		return leftNum.CompareTo(rightNum);
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
		foreach (var (index, name) in node.Names.WithIndex()) {
			var value = Evaluate(node.Values[index]);
			env.Define(name, value);
		}
		
		return null;
	}

	public object VisitAssignment(AssignmentNode node) {
		var value = Evaluate(node.Value);
		env.Set(node.Name, value);

		return value;
	}

	public object VisitShorthandAssignment(ShorthandAssignmentNode node) {
		object currentValue = env.Get(node.Name);
		object newValue;

		switch (node.Op) {
			case "+=":
				if (node.Value == null) throw new RuntimeError("'+=' requires a value");
				newValue = HandleAddition(currentValue, Evaluate(node.Value));
				break;
            
			case "-=":
				if (node.Value == null) throw new RuntimeError("'-=' requires a value");
				newValue = Convert.ToDouble(currentValue) - Convert.ToDouble(Evaluate(node.Value));
				break;
            
			case "*=":
				if (node.Value == null) throw new RuntimeError("'*=' requires a value");
				newValue = HandleMultiplication(currentValue, Evaluate(node.Value));
				break;
            
			case "/=":
				if (node.Value == null) throw new RuntimeError("'/=' requires a value");
				newValue = Convert.ToDouble(currentValue) / Convert.ToDouble(Evaluate(node.Value));
				break;
            
			case "^=":
				if (node.Value == null) throw new RuntimeError("'^=' requires a value");
				newValue = Math.Pow(Convert.ToDouble(currentValue), Convert.ToDouble(Evaluate(node.Value)));
				break;
            
			case "%=":
				if (node.Value == null) throw new RuntimeError("'%=' requires a value");
				newValue = Convert.ToDouble(currentValue) % Convert.ToDouble(Evaluate(node.Value));
				break;
            
			case "++":
				newValue = Convert.ToDouble(currentValue) + 1;
				break;
            
			case "--":
				newValue = Convert.ToDouble(currentValue) - 1;
				break;
            
			default:
				throw new RuntimeError($"Unknown shorthand operator: {node.Op}");
		}

		env.Set(node.Name, newValue);
		return newValue;
	}
	
	public object? VisitIF(IFNode node) {
		if (IsTruthy(Evaluate(node.Condition))) {
			env.Push();
			foreach (var n in node.Then) {
				Evaluate(n);
			}

			env.Pop();

			return null;
		}

		for (int i = 0; i < node.Elif.Length; i++) {
			if (IsTruthy(Evaluate(node.Elif[i].Item1))) {
				env.Push();
				foreach (var n in node.Elif[i].Item2) {
					Evaluate(n);
				}

				env.Pop();

				return null;
			}
		}

		if (node.ElseStatements.Length > 0) {
			env.Push();
			foreach (var n in node.ElseStatements) {
				Evaluate(n);
			}

			env.Pop();
		}

		return null;
	}

	public object? VisitScope(ScopeNode node) {
		env.Push();

		foreach (var statement in node.Statements) {
			var value = Evaluate(statement);

			if (statement is ReturnNode) {
				env.Pop();
				return value;
			}
		}
			
		env.Pop();

		return null;
	}
	
	public object? VisitFunctionCall(FunctionCall node) {
		object?[] args = new object?[node.Args.Length];

		for (int i = 0; i < args.Length; i++) {
			args[i] = Evaluate(node.Args[i]);
		}

		switch (node.Name) {
			case "print": return Builtin.BuiltinFunctions.Print(args);
			case "println": return Builtin.BuiltinFunctions.Print(args, true);
		}

		FunctionNode? func = null;
		// Check named function first
		if (!string.IsNullOrEmpty(node.Name) && env.Get(node.Name) is FunctionNode namedFunc) {
			func = namedFunc;
		}
		// Check target function (lambda/IIFE)
		else if (node.Target != null && Evaluate(node.Target) is FunctionNode targetFunc) {
			func = targetFunc;
		}
		
		if (func != null) {
			env.Push();

			foreach (var (index, arg) in func.Args.WithIndex()) {
				env.Define(arg, Evaluate(node.Args[index]));
			}

			foreach (var statement in func.Statements) {
				var value = Evaluate(statement);

				if (statement is ReturnNode) {
					env.Pop();
					return value;
				}
			}
			
			env.Pop();
		}
		
		return null;
	}

	public object? VisitFunctionDef(FunctionDef node) {
		env.Define(node.Name, new FunctionNode(node.Args, node.Statements));
		return null;
	}

	public object VisitReturn(ReturnNode node) {
		return Evaluate(node.Value);
	}
}

public static class EnumerableExtensions {
	public static IEnumerable<(int Index, T Item)> WithIndex<T>(this IEnumerable<T> source) {
		return source.Select((item, index) => (index, item));
	}
}