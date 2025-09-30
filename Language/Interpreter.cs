using System.Text.RegularExpressions;
using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Namespaces;
using Cranberry.Nodes;
using Cranberry.Types;

// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable DuplicatedStatements

namespace Cranberry;

public partial class Interpreter : INodeVisitor<object> {
	public Env env = new();
	private readonly Dictionary<string, CNamespace> Namespaces = new();

	private const double TOLERANCE = 1e-9;

	public Interpreter() {
		Namespaces.Add("Std", new StandardNamespace(this));
	}

	public object Evaluate(Node node) {
		return node.Accept(this)!;
	}

	//////////////////////////////////////////
	// TYPES
	//////////////////////////////////////////

	public object VisitNumber(NumberNode node) => node.Value;
	public object VisitNull(NullNode node) => node;
	public object VisitBool(BoolNode node) => node.Value;
	public object VisitString(StringNode node) => new CString(node.Value);

	public object VisitFunction(FunctionNode node) {
		node.Env = env.Variables.Peek();
		return node;
	}

	public CRange VisitRange(RangeNode node) {
		return new CRange(
			Convert.ToDouble(Evaluate(node.Start)),
			Convert.ToDouble(Evaluate(node.End)),
			Convert.ToDouble(Evaluate(node.Step) switch {
				NullNode => 1,
				{ } value => value
			}),
			node.Inclusive
		);
	}

	public CList VisitList(ListNode node) {
		if (node.Items.Count > 0 && Evaluate(node.Items[0]) is List<object> l) {
			return new CList(l.Select(x => x is Node a ? Evaluate(a) : x).ToList());
		}

		return new CList(node.Items.Select(Evaluate).ToList());
	}

	public object VisitInternalFunction(InternalFunction node) => node;

	public CDict VisitDict(DictNode node) {
		return new CDict(node.Items.Select((item, _) => {
			var key = Evaluate(item.Key);
			if (key is CString c)
				return (c.Value, Evaluate(item.Value));

			return (key, Evaluate(item.Value));
		}).ToDictionary());
	}

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

	private static object HandleAddition(object left, object right) {
		// String concatenation
		if (left is string && right is string) {
			return $"{left}{right}";
		}

		// Number addition
		if (Misc.IsNumber(left) && Misc.IsNumber(right))
			return Convert.ToDouble(left) + Convert.ToDouble(right);

		throw new RuntimeError($"Cannot add {Misc.FormatValue(left, true)} and {Misc.FormatValue(right)}.");
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
		var value = Evaluate(node.Value);
		if (node.Op == "$" && value is string template) {
			string result = MyRegex().Replace(template, match => {
				string key = match.Groups[1].Value;
				return Evaluate(
						new Parser(
							new Lexer(key).GetTokens().ToArray()
						).ParseExpression()
					)
					.ToString()!;
			});

			return result;
		}

		var u_value = Convert.ToDouble(value);

		return node.Op switch {
			"-" => -u_value,
			"+" => u_value,

			_ => throw new RuntimeError($"Unknown unary expression: {node.Op}")
		};
	}

	public object? VisitMemberAccess(MemberAccessNode node) {
		var target = Evaluate(node.Target);

		if (target is IMemberAccessible access) {
			var member = Evaluate(node.Member);
			if (member is CString str)
				return access.GetMember(str.Value);

			return access.GetMember(member);
		}

		throw new RuntimeError($"Cannot access member '{node.Member}' on value '{target}'");
	}
	
	public object VisitMemberAssignment(MemberAssignmentNode node) {
		var target = Evaluate(node.Target);

		if (target is IMemberAccessible access) {
			access.SetMember(Evaluate(node.Member), Evaluate(node.Value));
			return new NullNode();
		}

		throw new RuntimeError($"Cannot access member '{node.Member}' on value '{target}'");
	}
	
	public object VisitMemberShorthandAssignment(MemberShorthandAssignmentNode node) {
		var target = Evaluate(node.Target);

		if (target is IMemberAccessible access) {
			var currentValue = Evaluate(node.Value);
			var other = Evaluate(new MemberAccessNode(node.Target, node.Member));
			object newValue;
				
			switch (node.Op) {
				case "+=":
					if (node.Value == null) throw new RuntimeError("'+=' requires a value");
					newValue = HandleAddition(currentValue, other);
					break;

				case "-=":
					if (node.Value == null) throw new RuntimeError("'-=' requires a value");
					newValue = Convert.ToDouble(currentValue) - Convert.ToDouble(other);
					break;

				case "*=":
					if (node.Value == null) throw new RuntimeError("'*=' requires a value");
					newValue = HandleMultiplication(currentValue, other);
					break;

				case "/=":
					if (node.Value == null) throw new RuntimeError("'/=' requires a value");
					newValue = Convert.ToDouble(currentValue) / Convert.ToDouble(other);
					break;

				case "^=":
					if (node.Value == null) throw new RuntimeError("'^=' requires a value");
					newValue = Math.Pow(Convert.ToDouble(currentValue), Convert.ToDouble(other));
					break;

				case "%=":
					if (node.Value == null) throw new RuntimeError("'%=' requires a value");
					newValue = Convert.ToDouble(currentValue) % Convert.ToDouble(other);
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
			
			access.SetMember(Evaluate(node.Member), newValue);
			return new NullNode();
		}

		throw new RuntimeError($"Cannot access member '{node.Member}' on value '{target}'");
	}

	public object VisitFallback(FallbackNode node) {
		var left = Evaluate(node.Left);
		return Misc.IsTruthy(left) ? left : Evaluate(node.Right);
	}

	public object VisitCast(CastNode node) {
		object value;
		if (node.ToCast is Node n)
			value = Evaluate(n);
		else {
			value = node.ToCast;
		}
		
		switch (node.Type) {
			case "string": return new CString(BuiltinFunctions.ToString(value)!);
			case "number": return BuiltinFunctions.ToNumber(value);
			case "bool": return Misc.IsTruthy(value);
			case "char": {
				if (Misc.IsNumber(value))
					return (char)Convert.ToByte(value);
				
				if (value is CString c)
					return Convert.ToChar(c.Value);

				try {
					return (char)value;
				} catch {
					// ignored
				}
				break;
			}

			case "list": {
				if (value is CList) return value;
				if (value is CDict dict) return dict.Items.Values;
				if (value is string s) return new CList(s.ToCharArray().Select(object (x) => new CString(x.ToString())).ToList());

				break;
			}
		}

		throw new RuntimeError($"Cannot cast to type {node.Type}.");
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
		if (Misc.IsTruthy(Evaluate(node.Condition))) {
			env.Push();
			try {
				return Evaluate(node.Then);
			} catch (OutException re) {
				return re.Value;
			} finally {
				env.Pop();
			}
		}

		for (int i = 0; i < node.Elif.Length; i++) {
			if (Misc.IsTruthy(Evaluate(node.Elif[i].Item1))) {
				env.Push();
				try {
					return Evaluate(node.Elif[i].Item2);
				} catch (OutException re) {
					return re.Value;
				} finally {
					env.Pop();
				}
			}
		}

		if (node.ElseStatements != null) {
			env.Push();
			try {
				return Evaluate(node.ElseStatements);
			} catch (OutException re) {
				return re.Value;
			} finally {
				env.Pop();
			}
		}

		return new NullNode();
	}

	public object? VisitBlock(BlockNode node) {
		foreach (var statement in node.Statements) {
			Evaluate(statement);
		}

		return null;
	}

	public object? VisitScope(ScopeNode node) {
		env.Push();
		try {
			return Evaluate(node.Block);
		} catch (OutException re) {
			return re.Value;
		} finally {
			env.Pop();
		}
	}

	public object? VisitFunctionCall(FunctionCall node) {
		// build arg list (evaluated)
		var args = new List<object>(node.Args.Length);
		args.AddRange(node.Args.Select(x => {
			if (x is Node n) return Evaluate(n);
			return x;
		})!);

		switch (node.Name) {
			case "print": return BuiltinFunctions.Print(args);
			case "println": return BuiltinFunctions.Print(args, true);
			case "format": return BuiltinFunctions.Format(args);
			case "List": {
				if (args.Count == 1) {
					return new CList([args[0]]);
				}
				
				if (args is [_, double d]) return new CList(new object[Convert.ToInt32(d)].Select(_ => args[0]).ToList());

				if (args.Count == 0)
					return new CList([]);

				throw new RuntimeError("List() got invalid arguments. (It can take no arguments, a value, and optional size amount.)");
			}
			case "Dict": return new CDict(new Dictionary<object, object>());
		}

		FunctionNode? func = null;

		if (node.Target != null) {
			var target = Evaluate(node.Target);

			// class called as target: MyModule.MyClass(...)
			if (target is CClass cTarget) {
				var create = cTarget.GetCreateFunction();
				try {
					return create.Call(args.ToArray());
				} catch (ReturnException re) {
					return re.Value;
				} catch (OutException re) {
					return re.Value;
				}
			}

			if (target is InternalFunction f) {
				try {
					return f.Call(args.ToArray());
				} catch (ReturnException re) {
					return re.Value;
				} catch (OutException re) {
					return re.Value;
				}
			}

			if (target is ObjectMethod o) {
				args.Insert(0, o.Target);
				func = o.Func;
			} else if (target is FunctionNode targetFunc) {
				func = targetFunc;
			}
		} else {
			// No target: could be named function OR a bare class call: MyClass(...)
			if (!string.IsNullOrEmpty(node.Name)) {
				var lookup = env.Get(node.Name);
				if (lookup is FunctionNode namedFunc) {
					func = namedFunc;
				} else if (lookup is CClass namedClass) {
					// class called by name: MyClass(...)
					var create = namedClass.GetCreateFunction();
					try {
						return create.Call(args.ToArray());
					} catch (ReturnException re) {
						return re.Value;
					} catch (OutException re) {
						return re.Value;
					}
				} else if (lookup is InternalFunction internalF) {
					try {
						return internalF.Call(args.ToArray());
					} catch (ReturnException re) {
						return re.Value;
					} catch (OutException re) {
						return re.Value;
					}
				}
			}
		}

		if (func != null) {
			env.Push();
			try {
				env.Push(func.Env!);

				// safe binding of args -> default to NullNode when missing
				for (int i = 0; i < func.Args.Length; i++) {
					object val = i < args.Count ? args[i] : new NullNode();
					env.Define(func.Args[i], val);
				}

				try {
					var rv = Evaluate(func.Block);
					return rv;
				} catch (ReturnException re) {
					return re.Value;
				} catch (OutException re) {
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

	public object VisitOut(OutNode node) {
		if (node.Value != null) {
			throw new OutException(Evaluate(node.Value));
		}

		throw new OutException(null);
	}

	public object VisitContinue(ContinueNode node) {
		throw new ContinueException();
	}

	public object? VisitWhile(WhileNode node) {
		var ReturnValues = new List<object>();

		while (Misc.IsTruthy(Evaluate(node.Condition))) {
			env.Push();
			try {
				Evaluate(node.Block);
			} catch (BreakException be) {
				return be.Value;
			} catch (OutException be) {
				if (be.Value != null)
					ReturnValues.Add(be.Value);
			} catch (ContinueException) {
			}

			env.Pop();
		}

		return ReturnValues.Count > 0 ? ReturnValues : new NullNode();
	}

	public object? VisitFOR(ForNode node) {
		var iterable = Evaluate(node.Iterable);
		var ReturnValues = new List<object>();

		if (iterable is CRange range) {
			var step = range.Step;

			for (double i = Convert.ToDouble(range.Start);
			     (Convert.ToDouble(step) < 0) switch {
				     true => range.Inclusive switch {
					     true => i >= Convert.ToDouble(range.End),
					     _ => i > Convert.ToDouble(range.End)
				     },
				     _ => range.Inclusive switch {
					     true => i <= Convert.ToDouble(range.End),
					     _ => i < Convert.ToDouble(range.End)
				     }
			     };
			     i += Convert.ToDouble(step)
			    ) {
				env.Push();
				try {
					env.Define(node.VarName, i);
					Evaluate(node.Block);
				} catch (BreakException be) {
					return be.Value;
				} catch (OutException be) {
					if (be.Value != null)
						ReturnValues.Add(be.Value);
				} catch (ContinueException) {
				} finally {
					env.Pop();
				}
			}

			return ReturnValues.Count > 0 ? ReturnValues : new NullNode();
		}

		if (iterable is CList list) {
			foreach (var i in list.Items) {
				env.Push();
				try {
					env.Define(node.VarName, i is Node a ? Evaluate(a) : i);
					Evaluate(node.Block);
				} catch (BreakException be) {
					return be.Value;
				} catch (OutException be) {
					if (be.Value != null)
						ReturnValues.Add(be.Value);
				} catch (ContinueException) {
				} finally {
					env.Pop();
				}
			}

			return ReturnValues.Count > 0 ? ReturnValues : new NullNode();
		}

		if (iterable is string str) {
			foreach (var i in str) {
				env.Push();
				try {
					env.Define(node.VarName, i.ToString());
					Evaluate(node.Block);
				} catch (BreakException be) {
					return be.Value;
				} catch (OutException be) {
					if (be.Value != null)
						ReturnValues.Add(be.Value);
				} catch (ContinueException) {
				} finally {
					env.Pop();
				}
			}

			return ReturnValues.Count > 0 ? ReturnValues : new NullNode();
		}

		throw new RuntimeError($"Cannot loop over `{Evaluate(node.Iterable).GetType()}`. Expected iterable.");
	}

	public object? VisitSwitch(SwitchNode node) {
		var value = Evaluate(node.Expr);

		foreach (var (cases, block) in node.Cases) {
			foreach (var expr in cases) {
				if (value.Equals(Evaluate(expr))) {
					env.Push();
					try {
						return Evaluate(block);
					} catch (OutException re) {
						return re.Value;
					} finally {
						env.Pop();
					}
				}
			}
		}

		if (node.DefaultCase != null) {
			env.Push();
			try {
				Evaluate(node.DefaultCase);
			} catch (OutException re) {
				return re.Value;
			} finally {
				env.Pop();
			}
		}

		return new NullNode();
	}

	public object VisitClassDef(ClassDef node) {
		var class_value = new CClass(node.Name, node.Constructor, this);

		foreach (var f in node.Functions) {
			class_value.Functions.Add(f.Name, new FunctionNode(f.Args, f.Block));
		}

		env.Define(node.Name, class_value);
		return class_value;
	}

	public object? VisitUsingDirective(UsingDirective node) {
		var names = node.Names.GetEnumerator();
		names.MoveNext();

		CNamespace? latest = null;
		Env latest_env = env;
		var std = Namespaces["Std"];

		if (Namespaces.TryGetValue((string)names.Current!, out var value)) {
			latest = value;
			latest_env = value.env;
		}

		bool list_of_spaces = false;

		while (names.MoveNext()) {
			if (names.Current is string name) {
				if (latest == std) {
					latest = (CNamespace)latest.GetMember(name);
					break;
				}

				if (latest_env.HasNamespace(name)) {
					latest = latest_env.GetNamespace(name);
					latest_env = latest.env;
				} else {
					if (latest != null) {
						throw new RuntimeError($"Namespace `{name}` doesn't exist in `{latest.Name}`.");
					}

					throw new RuntimeError($"Namespace `{name}` doesn't exist.");
				}
			} else if (names.Current is string[] multiple) {
				list_of_spaces = true;

				foreach (var m in multiple) {
					if (latest == std) {
						if (node.Aliases.TryGetValue(m, out var alias))
							env.DefineNamespace((CNamespace)latest.GetMember(m), alias);
						else
							env.DefineNamespace((CNamespace)latest.GetMember(m));
					} else if (latest_env.HasNamespace(m)) {
						if (node.Aliases.TryGetValue(m, out var alias))
							env.DefineNamespace(latest_env.GetNamespace(m), alias);
						else
							env.DefineNamespace(latest_env.GetNamespace(m));
					} else {
						throw new RuntimeError($"Namespace `{m}` doesn't exist in `{latest!.Name}`.");
					}
				}
			}
		}

		if (!list_of_spaces && latest != null)
			if (node.Aliases.TryGetValue(latest.Name, out var alias))
				env.DefineNamespace(latest, alias);
			else
				env.DefineNamespace(latest);

		return null;
	}

	public object? VisitNamespaceDirective(NamespaceDirective node) {
		var names = node.Names.GetEnumerator();
		names.MoveNext();

		if ((string)names.Current! == "Std")
			throw new RuntimeError("Cannot override `Std` namespaces.");

		var space_name = (string)names.Current!;
		CNamespace? latest;

		if (Namespaces.TryGetValue(space_name, out var value)) {
			latest = value;
		} else {
			latest = new CNamespace(space_name);
			Namespaces.Add(space_name, latest);
		}

		env = latest.env;

		while (names.MoveNext()) {
			space_name = (string)names.Current!;

			var new_space = new CNamespace(space_name);
			latest.env.Namespaces.Add(space_name, new_space);

			latest = new_space;
			env = latest.env;
		}

		return null;
	}

	public object VisitIncludeDirective(IncludeDirective node) {
		var file_path = Evaluate(node.Paths);

		if (file_path is CList Paths) {
			foreach (var path in Paths.Items) {
				if (path is CString) continue;

				throw new RuntimeError("`include` only takes string path or `{paths}`.");
			}

			throw new IncludeFileException(Paths.Items.Select(object (x) => {
				if (x is CString c)
					return c.Value;
				return x;
			}));
		}

		if (file_path is string p)
			throw new IncludeFileException(p);

		throw new RuntimeError("`include` only takes string path or list of strings.");
	}

	[GeneratedRegex(@"\{(.*?)\}")]
	private static partial Regex MyRegex();
}

public static class EnumerableExtensions {
	public static IEnumerable<(int Index, T Item)> WithIndex<T>(this IEnumerable<T> source) {
		return source.Select((item, index) => (index, item));
	}
}