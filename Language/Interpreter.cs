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
	public object VisitNull(NullNode node) => node;
	public object VisitBool(BoolNode node) => node.Value;
	public object VisitString(StringNode node) => node.Value;
	public object VisitFunction(FunctionNode node) => node;
	public object VisitRange(RangeNode node) => node;

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

	private static object HandleAddition(object? left, object? right) {
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

	private static bool AreEqual(object? left, object? right) {
		if (left == null && right == null) return true;
		if (left == null || right == null) return false;

		// Float comparison with tolerance
		if (left is double leftDouble && right is double rightDouble) {
			return Math.Abs(leftDouble - rightDouble) < TOLERANCE;
		}

		return left.Equals(right);
	}

	private static int CompareValues(object? left, object? right) {
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
			try {
				return Evaluate(node.Then);
			} finally {
				env.Pop();
			}
		}

		for (int i = 0; i < node.Elif.Length; i++) {
			if (IsTruthy(Evaluate(node.Elif[i].Item1))) {
				env.Push();
				try {
					return Evaluate(node.Elif[i].Item2);
				} finally {
					env.Pop();
				}
			}
		}

		if (node.ElseStatements != null) {
			env.Push();
			try {
				return Evaluate(node.ElseStatements);
			} finally {
				env.Pop();
			}
		}

		return null;
	}

	public object? VisitBlock(BlockNode node) {
		foreach (var statement in node.Statements) {
			Evaluate(statement);
		}

		return null;
	}

	public object VisitScope(ScopeNode node) {
		env.Push();
		try {
			return Evaluate(node.Block);
		} finally {
			env.Pop();
		}
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

		if (!string.IsNullOrEmpty(node.Name) && env.Get(node.Name) is FunctionNode namedFunc) {
			func = namedFunc;
		} else if (node.Target != null && Evaluate(node.Target) is FunctionNode targetFunc) {
			func = targetFunc;
		}

		if (func != null) {
			env.Push();
			try {
				for (int i = 0; i < func.Args.Length; i++) {
					env.Define(func.Args[i], args[i]); // use args array
				}

				try {
					// Evaluate the function's scope; if it returns normally, we get null or a value
					var rv = Evaluate(func.Block);
					return rv;
				} catch (ReturnException re) {
					// A return inside the function body landed here
					return re.Value;
				}
			} finally {
				env.Pop();
			}
		}

		return new NullNode();
	}

	public object? VisitFunctionDef(FunctionDef node) {
		env.Define(node.Name, new FunctionNode(node.Args, node.Block));
		return null;
	}

	public object VisitReturn(ReturnNode node) {
		if (node.Value != null) {
			throw new ReturnException(Evaluate(node.Value));
		}

		throw new ReturnException(null);
	}

	public object VisitBreak(BreakNode node) {
		if (node.Value != null) {
			throw new BreakException(Evaluate(node.Value));
		}

		throw new BreakException(null);
	}

	public object? VisitWhile(WhileNode node) {
		while (IsTruthy(Evaluate(node.Condition))) {
			env.Push();
			try {
				Evaluate(node.Block);
			} catch (BreakException be) {
				return be.Value;
			} finally {
				env.Pop();
			}
		}

		return new NullNode();
	}

	public object? VisitFOR(ForNode node) {
		if (node.Iterable is RangeNode range) {
			var step = Evaluate(range.Step);
			if (step is null or NullNode)
				step = 1;
			
			for (double i = Convert.ToDouble(Evaluate(range.Start));
			     range.Inclusive switch {
				     true => i <= Convert.ToDouble(Evaluate(range.End)),
				     _ => i < Convert.ToDouble(Evaluate(range.End))
			     };
			     i += Convert.ToDouble(step)
			    ) {
				env.Push();
				try {
					env.Define(node.VarName, i);
					Evaluate(node.Block);
				} catch (BreakException be) {
					return be.Value;
				} finally {
					env.Pop();
				}
			}
		}

		return new NullNode();
	}
}

public static class EnumerableExtensions {
	public static IEnumerable<(int Index, T Item)> WithIndex<T>(this IEnumerable<T> source) {
		return source.Select((item, index) => (index, item));
	}
}