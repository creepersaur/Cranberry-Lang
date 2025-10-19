using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.External;
using Cranberry.Namespaces;
using Cranberry.Nodes;
using Cranberry.Types;

// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable DuplicatedStatements

namespace Cranberry;

public partial class Interpreter : INodeVisitor<object> {
	public Env env = new();
	public readonly bool IsBuild;
	private readonly Dictionary<string, CNamespace> Namespaces = new();
	private const double TOLERANCE = 1e-9;

	// --- assembly loading helpers ---
	private readonly Dictionary<string, Assembly> _loadedAssemblies = new(StringComparer.OrdinalIgnoreCase);

	public Interpreter(bool is_build) {
		IsBuild = is_build;
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
		// Capture the closure only once (when the function is defined),
		// don't overwrite it every time the node is evaluated.
		if (node.Env == null) {
			node.Env = env.Variables.Peek();
		}

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
			return new CList(l.Select(x => x is Node a ? Evaluate(a) : x).ToList(), node.IsTuple);
		}

		return new CList(node.Items.Select(Evaluate).ToList(), node.IsTuple);
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
			"-" => HandleSubtraction(leftVal, rightVal),
			"*" => HandleMultiplication(leftVal, rightVal),
			"/" => HandleDivision(leftVal, rightVal),
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

			"&&" => Misc.IsTruthy(leftVal) && Misc.IsTruthy(rightVal),
			"||" => Misc.IsTruthy(leftVal) || Misc.IsTruthy(rightVal),

			_ => throw new RuntimeError($"Unknown binary expression: {node.Op}")
		};
	}

	private static object HandleAddition(object left, object right) {
		// String concatenation
		if (left is CString l && right is CString r) {
			return new CString(l.Value + r.Value);
		}

		if (left is CObject cl) {
			if (cl.Class.Functions.TryGetValue("__add__", out var f)) {
				var string_func = new ObjectMethod(cl, f);
				var value = Program.interpreter!.Evaluate(new FunctionCall("", [string_func.Target, right]) {
					Target = string_func.Func
				});

				return value;
			}
		}

		if (right is CObject cr) {
			if (cr.Class.Functions.TryGetValue("__add__", out var f)) {
				var string_func = new ObjectMethod(cr, f);
				var value = Program.interpreter!.Evaluate(new FunctionCall("", [string_func.Target, left]) {
					Target = string_func.Func
				});

				return value;
			}
		}

		// Number addition
		if (Misc.IsNumber(left) && Misc.IsNumber(right))
			return Convert.ToDouble(left) + Convert.ToDouble(right);

		throw new RuntimeError($"Cannot add {Misc.FormatValue(left, true)} and {Misc.FormatValue(right, true)}.");
	}

	private static object HandleSubtraction(object left, object right) {
		if (left is CObject cl) {
			if (cl.Class.Functions.TryGetValue("__sub__", out var f)) {
				var string_func = new ObjectMethod(cl, f);
				var value = Program.interpreter!.Evaluate(new FunctionCall("", [string_func.Target, right]) {
					Target = string_func.Func
				});

				return value;
			}
		}

		if (right is CObject cr) {
			if (cr.Class.Functions.TryGetValue("__sub__", out var f)) {
				var string_func = new ObjectMethod(cr, f);
				var value = Program.interpreter!.Evaluate(new FunctionCall("", [string_func.Target, left]) {
					Target = string_func.Func
				});

				return value;
			}
		}

		// Number addition
		if (Misc.IsNumber(left) && Misc.IsNumber(right))
			return Convert.ToDouble(left) - Convert.ToDouble(right);

		throw new RuntimeError($"Cannot subtract {Misc.FormatValue(left, true)} and {Misc.FormatValue(right)}.");
	}

	private static object HandleMultiplication(object left, object right) {
		// String multiplication
		if (left is CString l && right is double) {
			return string.Concat(Enumerable.Repeat(l.Value, Convert.ToInt32(right)));
		}

		if (left is CObject cl) {
			if (cl.Class.Functions.TryGetValue("__mul__", out var f)) {
				var string_func = new ObjectMethod(cl, f);
				var value = Program.interpreter!.Evaluate(new FunctionCall("", [string_func.Target, right]) {
					Target = string_func.Func
				});

				return value;
			}
		}

		if (right is CObject cr) {
			if (cr.Class.Functions.TryGetValue("__mul__", out var f)) {
				var string_func = new ObjectMethod(cr, f);
				var value = Program.interpreter!.Evaluate(new FunctionCall("", [string_func.Target, left]) {
					Target = string_func.Func
				});

				return value;
			}
		}

		// Number addition
		if (Misc.IsNumber(left) && Misc.IsNumber(right))
			return Convert.ToDouble(left) * Convert.ToDouble(right);

		throw new RuntimeError($"Cannot multiply {Misc.FormatValue(left, true)} and {Misc.FormatValue(right)}.");
	}

	private static object HandleDivision(object left, object right) {
		if (left is CObject cl) {
			if (cl.Class.Functions.TryGetValue("__div__", out var f)) {
				var string_func = new ObjectMethod(cl, f);
				var value = Program.interpreter!.Evaluate(new FunctionCall("", [string_func.Target, right]) {
					Target = string_func.Func
				});

				return value;
			}
		}

		if (right is CObject cr) {
			if (cr.Class.Functions.TryGetValue("__div__", out var f)) {
				var string_func = new ObjectMethod(cr, f);
				var value = Program.interpreter!.Evaluate(new FunctionCall("", [string_func.Target, left]) {
					Target = string_func.Func
				});

				return value;
			}
		}

		// Number addition
		if (Misc.IsNumber(left) && Misc.IsNumber(right)) {
			if ((double)right == 0)
				throw new DivideByZeroException("Cannot divide by zero.");
			return Convert.ToDouble(left) / Convert.ToDouble(right);
		}

		throw new RuntimeError($"Cannot divide {Misc.FormatValue(left, true)} and {Misc.FormatValue(right)}.");
	}

	private static bool AreEqual(object? left, object? right) {
		if (left == null && right == null) return true;
		if (left == null || right == null) return false;

		// Float comparison with tolerance
		if (left is double leftDouble && right is double rightDouble) {
			return Math.Abs(leftDouble - rightDouble) < TOLERANCE;
		}

		if (left is CString csl) left = csl.Value;
		if (right is CString csr) right = csr.Value;

		if (left is CObject cl) {
			if (cl.Class.Functions.TryGetValue("__eq__", out var f)) {
				var string_func = new ObjectMethod(cl, f);
				var value = Program.interpreter!.Evaluate(new FunctionCall("", [string_func.Target, right]) {
					Target = string_func.Func
				});

				return Misc.IsTruthy(value);
			}
		}

		if (right is CObject cr) {
			if (cr.Class.Functions.TryGetValue("__eq__", out var f)) {
				var string_func = new ObjectMethod(cr, f);
				var value = Program.interpreter!.Evaluate(new FunctionCall("", [string_func.Target, left]) {
					Target = string_func.Func
				});

				return Misc.IsTruthy(value);
			}
		}

		return left.Equals(right);
	}

	private static int CompareValues(object? left, object? right) {
		// String comparison
		if (left is string leftStr && right is string rightStr) {
			return string.CompareOrdinal(leftStr, rightStr);
		}

		if (left is CString cleftStr && right is CString crightStr) {
			return string.CompareOrdinal(cleftStr.Value, crightStr.Value);
		}

		// Number comparison
		double leftNum = Convert.ToDouble(left);
		double rightNum = Convert.ToDouble(right);
		return leftNum.CompareTo(rightNum);
	}

	public object VisitUnaryOp(UnaryOpNode node) {
		var value = Evaluate(node.Value);

		if (node.Op == "$" && value is CString template) {
			string result = MyRegex().Replace(template.Value, match => {
				string key = match.Groups[1].Value;
				return Misc.FormatValue(Evaluate(
					new Parser(
						new Lexer(key).GetTokens().ToArray()
					).ParseExpression()
				))!;
			});

			return new CString(result);
		}

		return node.Op switch {
			"-" => -Convert.ToDouble(value),
			"+" => Convert.ToDouble(value),
			"!" => !Misc.IsTruthy(value),

			_ => throw new RuntimeError($"Unknown unary expression: {node.Op}")
		};
	}

	public object? VisitMemberAccess(MemberAccessNode node) {
		var target = Evaluate(node.Target);
		var member = Evaluate(node.Member);

		if (member is CString str)
			member = str.Value;

		if (target is IMemberAccessible access)
			return access.GetMember(member);

		if (member is string m) {
			var type = target.GetType();
			var prop = type.GetProperty(m, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

			if (prop != null)
				return prop.GetValue(target);
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
					newValue = HandleAddition(other, currentValue);
					break;

				case "-=":
					if (node.Value == null) throw new RuntimeError("'-=' requires a value");
					newValue = Convert.ToDouble(other) - Convert.ToDouble(currentValue);
					break;

				case "*=":
					if (node.Value == null) throw new RuntimeError("'*=' requires a value");
					newValue = HandleMultiplication(other, currentValue);
					break;

				case "/=":
					if (node.Value == null) throw new RuntimeError("'/=' requires a value");
					newValue = Convert.ToDouble(other) / Convert.ToDouble(currentValue);
					break;

				case "^=":
					if (node.Value == null) throw new RuntimeError("'^=' requires a value");
					newValue = Math.Pow(Convert.ToDouble(other), Convert.ToDouble(currentValue));
					break;

				case "%=":
					if (node.Value == null) throw new RuntimeError("'%=' requires a value");
					newValue = Convert.ToDouble(other) % Convert.ToDouble(currentValue);
					break;

				case "++":
					newValue = Convert.ToDouble(other) + 1;
					break;

				case "--":
					newValue = Convert.ToDouble(other) - 1;
					break;

				default:
					throw new RuntimeError($"Unknown shorthand operator: {node.Op}");
			}

			access.SetMember(Evaluate(node.Member), newValue);
			return newValue;
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
				if (value is CString c) return new CList(c.Value.ToCharArray().Select(object (x) => new CString(x.ToString())).ToList());
				if (value is string s) return new CList(s.ToCharArray().Select(object (x) => new CString(x.ToString())).ToList());
				if (value is CObject obj && obj.Class.Functions.TryGetValue("__next__", out var f)) {
					var list = new List<object>();
					var next = Evaluate(new FunctionCall("", [obj]) { Target = f });
					while (next is not NullNode) {
						list.Add(next);
						next = Evaluate(new FunctionCall("", [obj]) { Target = f });
					}

					return new CList(list);
				}

				break;
			}
		}

		throw new RuntimeError($"Cannot cast to type {node.Type}.");
	}

	//////////////////////////////////////////
	// STATEMENTS
	//////////////////////////////////////////

	public object? VisitLet(LetNode node) {
		object? first_value = null;
		foreach (var (index, name) in node.Names.WithIndex()) {
			var value = node.Values.Length > index ? Evaluate(node.Values[index]) : new NullNode();
			first_value ??= value;

			if (name is List<string> l) {
				if (value is CList { IsTuple: true } val_list) {
					foreach (var (l_index, l_name) in l.WithIndex()) {
						var l_value =
							l.Count > l_index
								? val_list.Items[l_index]
								: throw new RuntimeError("Failed to destructure tuple. Number of names is more than length of tuple.");

						if (node.Constant) env.DefineConstant(l_name, l_value);
						else env.Define(l_name, l_value);
					}
				} else throw new RuntimeError("Destructuring syntax only works with tuples.");
			} else {
				if (node.Constant) env.DefineConstant((string)name, value);
				else env.Define((string)name, value);
			}
		}

		return first_value;
	}

	public object VisitAssignment(AssignmentNode node) {
		var values = new object[node.Values.Length];
		for (int i = 0; i < values.Length; i++) {
			values[i] = Evaluate(node.Values[i]);
		}

		for (int i = 0; i < node.Names.Length; i++)
			env.Set(node.Names[i], values[i]);

		return node.Values[0];
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
			case "typeof": return BuiltinFunctions.Typeof(args);
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
			var target = Evaluate(node.Target!);

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

			if (target is CObject co) {
				if (co.Class.Functions.TryGetValue("__call__", out var f)) {
					return Evaluate(new FunctionCall("", [co, ..args]) {
						Target = f
					});
				}
			}

			if (target is InternalFunction i) {
				try {
					return i.Call(args.ToArray());
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

			if (target is ExternFunction ef) {
				// convert Cranberry runtime values into plain CLR values expected by wrapper
				var clrArgs = args.Select(ConvertCLR.ToClr).ToArray();
				var resultClr = ef.Invoke(clrArgs);
				// convert back into Cranberry runtime type
				return ConvertCLR.ToCranberry(resultClr!);
			}
		} else {
			// No target: could be named function OR a bare class call: MyClass(...)
			if (!string.IsNullOrEmpty(node.Name)) {
				var lookup = env.Get(node.Name);
				if (lookup is FunctionNode namedFunc) {
					func = namedFunc;
				} else if (lookup is CClass namedClass) {
					var create = namedClass.GetCreateFunction();
					try {
						return create.Call(args.ToArray());
					} catch (ReturnException re) {
						return re.Value;
					} catch (OutException re) {
						return re.Value;
					}
				} else if (lookup is CObject co) {
					if (co.Class.Functions.TryGetValue("__call__", out var f)) {
						return Evaluate(new FunctionCall("", [co, ..args]) {
							Target = f
						});
					}

					throw new RuntimeError($"Cannot call value: `{Misc.FormatValue(co)}`");
				} else if (lookup is InternalFunction internalF) {
					try {
						return internalF.Call(args.ToArray());
					} catch (ReturnException re) {
						return re.Value;
					} catch (OutException re) {
						return re.Value;
					}
				} else if (lookup is ExternFunction ef) {
					// convert Cranberry runtime values into plain CLR values expected by wrapper
					var clrArgs = args.Select(ConvertCLR.ToClr).ToArray();
					var resultClr = ef.Invoke(clrArgs);
					// convert back into Cranberry runtime type
					return ConvertCLR.ToCranberry(resultClr!);
				}
			}
		}

		if (func != null) {
			env.Push();
			try {
				env.Push(func.Env!);
				try {
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
					env.Pop(); // pop func.Env!
				}
			} finally {
				env.Pop(); // pop the initial push()
			}
		}

		throw new RuntimeError($"Cannot call value `{Misc.FormatValue(node.Target!)}`");
	}

	public object? VisitFunctionDef(FunctionDef node) {
		env.Define(node.Name, new FunctionNode(node.Args, node.Block!));
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
					var value = i is Node a ? Evaluate(a) : i;
					if (value is string s) value = new CString(s);

					env.Define(node.VarName, value);
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

		if (iterable is CString c) {
			foreach (var i in c.Value) {
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

		if (iterable is CObject obj) {
			if (obj.Class.Functions.TryGetValue("__next__", out var f)) {
				while (true) {
					env.Push();
					try {
						var value = Evaluate(new FunctionCall("", [obj]) { Target = f });
						if (value is NullNode)
							return ReturnValues.Count > 0 ? ReturnValues : new NullNode();

						env.Define(node.VarName, value);
						Evaluate(node.Block);
					} catch (BreakException be) {
						return be.Value ?? (ReturnValues.Count > 0 ? ReturnValues : new NullNode());
					} catch (OutException be) {
						if (be.Value != null)
							ReturnValues.Add(be.Value);
					} catch (ContinueException) {
					} finally {
						env.Pop();
					}
				}
			}
		}

		throw new RuntimeError($"Cannot loop over `{Evaluate(node.Iterable).GetType()}`. Expected iterable.");
	}

	public object? VisitSwitch(SwitchNode node) {
		var value = Evaluate(node.Expr);

		foreach (var (cases, block) in node.Cases) {
			foreach (var expr in cases) {
				var cond = Evaluate(expr);

				if ((value is CString v && cond is CString c && v.Value == c.Value) || value.Equals(cond)) {
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
			class_value.Functions.Add(f.Name, new FunctionNode(f.Args, f.Block!));
		}

		foreach (var l in node.Lets) {
			class_value.Lets.Add(l);
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
						var latest_space = (CNamespace)latest.GetMember(m);
						if (node.Aliases.TryGetValue(m, out var alias))
							env.DefineNamespace(latest_space, alias);
						else {
							if (node.Wildcards.Contains(latest_space.Name))
								env.DefineWildcardNamespace(latest_space);
							else
								env.DefineNamespace(latest_space);
						}
					} else if (latest_env.HasNamespace(m)) {
						var latest_space = latest_env.GetNamespace(m);
						if (node.Aliases.TryGetValue(m, out var alias)) {
							env.DefineNamespace(latest_space, alias);
						} else {
							if (node.Wildcards.Contains(latest_space.Name))
								env.DefineWildcardNamespace(latest_space);
							else
								env.DefineNamespace(latest_space);
						}
					} else {
						throw new RuntimeError($"Namespace `{m}` doesn't exist in `{latest!.Name}`.");
					}
				}
			}
		}

		if (!list_of_spaces && latest != null)
			if (node.Aliases.TryGetValue(latest.Name, out var alias))
				env.DefineNamespace(latest, alias);
			else {
				if (node.Wildcards.Contains(latest.Name))
					env.DefineWildcardNamespace(latest);
				else
					env.DefineNamespace(latest);
			}

		return null;
	}

	public object? VisitNamespaceDirective(NamespaceDirective node) {
		var names = node.Names.GetEnumerator();
		names.MoveNext();

		if ((string)names.Current! == "Std")
			throw new RuntimeError("Cannot override `Std` namespaces.");

		var space_name = (string)names.Current!;
		CNamespace? latest;
		Env original = env;

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

		if (node.Block is { } b) {
			Evaluate(b);
			env = original;

			Evaluate(new UsingDirective(node.Names, new Dictionary<string, string>(), []));
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
				if (x is CString cs)
					return cs.Value;
				return x;
			}));
		}

		if (file_path is string p)
			throw new IncludeFileException(p);

		if (file_path is CString c)
			throw new IncludeFileException(c.Value);

		throw new RuntimeError("`include` only takes string path or list of strings.");
	}

	public object? VisitDecorator(DecoratorNode node) {
		if (string.Equals(node.Name, "extern_all", StringComparison.OrdinalIgnoreCase)) {
			if (node.Args.Length < 1)
				throw new RuntimeError("@extern_all(path) expects a path to the `.dll`.");

			ImportAssemblyFunctions(Evaluate(node.Args[0]).ToString()!);
			ImportAssemblyClasses(Evaluate(node.Args[0]).ToString()!);

			return null;
		}

		// Expect decorator name "extern"
		if (!string.Equals(node.Name, "extern", StringComparison.OrdinalIgnoreCase)) {
			throw new RuntimeError("Unknown decorator. Only @extern is known as of now.");
		}

		// Evaluate decorator args to get module path (assume first arg is DLL path string)
		if (node.Args.Length < 1)
			throw new RuntimeError("extern decorator expects at least one argument: the path to the DLL.");

		// Evaluate the first arg expression to runtime value (string).
		// We call Accept(this) to evaluate the Node — your interpreter probably has a VisitStringLiteral that returns string.
		var moduleVal = Evaluate(node.Args[0]);
		if (!(moduleVal is CString modulePath))
			throw new RuntimeError("extern decorator's first argument must be a string literal path to the DLL.");

		// Optional: second argument can be explicit function name inside the DLL (otherwise use the Cranberry function name)
		string? explicitSymbol = null;
		if (node.Args.Length >= 2) {
			var symVal = Evaluate(node.Args[1]);
			if (symVal is string s) explicitSymbol = s;
			else throw new Exception("extern decorator's second argument (symbol name) must be a string.");
		}

		var funcName = node.Func!.Name; // the Cranberry function name

		var symbolToFind = explicitSymbol ?? funcName;

		// Attempt to register the method (this will throw with helpful messages if it fails)
		ExternalManager.RegisterManagedFunctionFromAssembly(modulePath.Value, symbolToFind);

		// After registration, resolve wrapper and get MethodInfo parameter count to set arity.
		if (!ExternalManager.TryResolve(modulePath.Value, symbolToFind, out var wrapper))
			throw new Exception($"Failed to register external function {symbolToFind} from {modulePath.Value}");

		// Determine arity: we can try inspect the method signature via reflection again, but we didn't expose MethodInfo.
		// For simplicity: derive arity from the FunctionDef signature node.Func.Args
		var arity = node.Func.Args.Length;

		var extFunc = new ExternFunction(modulePath.Value, symbolToFind, wrapper!, arity);

		// Define in the environment under the original function name
		env.Define(funcName, extFunc);

		return null;
	}


	[GeneratedRegex(@"\{(.*?)\}")]
	private static partial Regex MyRegex();

	private Assembly LoadAssemblySafe(string modulePath) {
		if (!File.Exists(modulePath))
			throw new FileNotFoundException($"DLL not found: {modulePath}");


		if (_loadedAssemblies.TryGetValue(modulePath, out var existing))
			return existing;


// Create resolver for this path
		var resolver = new AssemblyDependencyResolver(modulePath);
		// _resolvers[modulePath] = resolver;


		Assembly? asm;
		try {
// Hook up a resolving handler that uses the resolver to find dependency paths
			AssemblyLoadContext.Default.Resolving += (context, name) => {
				try {
					var depPath = resolver.ResolveAssemblyToPath(name);
					if (!string.IsNullOrEmpty(depPath) && File.Exists(depPath)) {
						return context.LoadFromAssemblyPath(depPath);
					}
				} catch {
// swallow here and let fallback occur
				}


// last ditch: probe same directory for simple name
				var probe = Path.Combine(Path.GetDirectoryName(modulePath)!, name.Name + ".dll");
				if (File.Exists(probe)) return context.LoadFromAssemblyPath(probe);
				return null;
			};


			asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(modulePath);
			_loadedAssemblies[modulePath] = asm;
			return asm;
		} catch (ReflectionTypeLoadException rtl) {
			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"Failed to load assembly {modulePath}: {rtl.Message}");
			foreach (var le in rtl.LoaderExceptions) {
				sb.AppendLine(le!.Message);
			}

			throw new RuntimeError(sb.ToString());
		} catch (Exception ex) {
			throw new RuntimeError($"Failed to load assembly {modulePath}: {ex.Message}");
		}
	}

	public void ImportAssemblyFunctions(string modulePath) {
// Accept either absolute path or relative path; expand and validate
		var absPath = Path.GetFullPath(modulePath);
		if (!File.Exists(absPath)) throw new FileNotFoundException($"DLL not found: {absPath}");


		var asm = LoadAssemblySafe(absPath);


// Prefer if ExternalManager exposes an Assembly-aware API. Try both.
		try {
// If ExternalManager has an Assembly-based register, use it (preferred)
			var method = typeof(ExternalManager).GetMethod("RegisterAllManagedFunctionsFromAssembly", new[] { typeof(Assembly) });
			if (method != null) {
				var wrappers = (Dictionary<string, Delegate>)method.Invoke(null, new object[] { asm })!;
				// 'wrappers' is Dictionary<string, Delegate> from ExternalManager
				foreach (var kv in wrappers) {
					var methodName = kv.Key;
					var rawDelegate = kv.Value; // System.Delegate

					// Try infer arity from MethodInfo if possible
					int arity;
					try {
						var mi = rawDelegate.Method;
						arity = mi.GetParameters().Length;
					} catch {
						arity = -1;
					}

					// Ensure we pass a Func<object[], object> to ExternFunction.
					Func<object[], object> wrapperFunc;

					if (rawDelegate is Func<object[], object> alreadyGood) {
						wrapperFunc = alreadyGood;
					} else {
						// Adapter: convert incoming cranberry args -> CLR args (best-effort) and call underlying delegate.
						wrapperFunc = (incomingArgs) => {
							try {
								// If the delegate target method has parameter info, try to convert each argument.
								var mi = rawDelegate.Method;
								var pars = mi.GetParameters();

								object[] invokeArgs;
								if (pars.Length == (incomingArgs.Length)) {
									invokeArgs = new object[pars.Length];
									for (int i = 0; i < pars.Length; i++) {
										// Use your ExternalManager conversion helper which knows how to convert Cranberry -> CLR
										try {
											invokeArgs[i] = ExternalManager.ConvertToClr(incomingArgs[i], pars[i].ParameterType) ?? incomingArgs[i];
										} catch {
											var wrappersFallback = ExternalManager.RegisterAllManagedFunctionsFromAssembly(absPath);
											foreach (var new_kv in wrappersFallback) {
												var new_methodName = new_kv.Key;
												var new_rawDelegate = new_kv.Value;

												int new_arity;
												try {
													new_arity = new_rawDelegate.Method.GetParameters().Length;
												} catch {
													new_arity = -1;
												}

												Func<object[], object> new_wrapperFunc;
												if (new_rawDelegate is { } f) new_wrapperFunc = f;
												else {
													new_wrapperFunc = (new_incomingArgs) => {
														try {
															var new_pars = new_rawDelegate.Method.GetParameters();
															object[] new_invokeArgs;
															if (new_pars.Length == (new_incomingArgs.Length)) {
																new_invokeArgs = new object[new_pars.Length];
																for (int j = 0; j < new_pars.Length; j++) {
																	try {
																		new_invokeArgs[j] = ExternalManager.ConvertToClr(new_incomingArgs[j], new_pars[j].ParameterType) ?? new_incomingArgs[j];
																	} catch {
																		new_invokeArgs[j] = new_incomingArgs[j];
																	}
																}
															} else {
																new_invokeArgs = new_incomingArgs;
															}

															var result = new_rawDelegate.DynamicInvoke(new_invokeArgs);
															return result!;
														} catch (TargetInvocationException tie) {
															throw tie.InnerException ?? tie;
														}
													};
												}

												var ef = new ExternFunction(absPath, new_methodName, new_wrapperFunc, new_arity);
												env.Define(new_methodName, ef);
											}
										}
									}
								} else {
									invokeArgs = incomingArgs.Select(a => {
										try {
											return ExternalManager.ConvertToClr(a, typeof(object)) ?? a;
										} catch {
											return a;
										}
									}).ToArray();
								}

								var result = rawDelegate.DynamicInvoke(invokeArgs);
								return result!;
							} catch (TargetInvocationException tie) {
								// Unwrap to provide clearer error to Cranberry runtime
								throw tie.InnerException ?? tie;
							}
						};
					}

					// Now create ExternFunction using new_wrapperFunc (a Func<object[],object>)
					var ef = new ExternFunction(absPath, methodName, wrapperFunc, arity);
					env.Define(methodName, ef);
				}

				return;
			}
		} catch (TargetInvocationException tie) {
			throw new RuntimeError($"Error registering functions from {absPath}: {tie.InnerException?.Message ?? tie.Message}");
		}


// Fallback: try the path-based API
		var wrappersFallback = ExternalManager.RegisterAllManagedFunctionsFromAssembly(absPath);
		foreach (var kv in wrappersFallback) {
			var methodName = kv.Key;
			var wrapper = kv.Value;
			int arity = -1;
			var ef = new ExternFunction(absPath, methodName, wrapper, arity);
			env.Define(methodName, ef);
		}
	}

	public void ImportAssemblyClasses(string modulePath) {
		if (!File.Exists(modulePath)) throw new FileNotFoundException(modulePath);

		var asm = Assembly.LoadFrom(modulePath);

		// Helper to register a type
		void RegisterType(Type t) {
			if (t.ContainsGenericParameters) return;
			if (!t.IsPublic) return;

			// Factory for creating instances
			object? Factory(object?[] callArgs) {
				var ctors = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
					.OrderByDescending(c => c.GetParameters().Length)
					.ToArray();

				foreach (var ctor in ctors) {
					var pars = ctor.GetParameters();
					if (pars.Length != (callArgs?.Length ?? 0)) continue;

					var invokeArgs = new object[pars.Length];
					bool ok = true;

					for (int i = 0; i < pars.Length; i++) {
						try {
							invokeArgs[i] = ExternalManager.ConvertToClr(callArgs![i], pars[i].ParameterType)!;
						} catch {
							ok = false;
							break;
						}
					}

					if (!ok) continue;

					try {
						return ctor.Invoke(invokeArgs);
					} catch (Exception ex) {
						Console.WriteLine($"Constructor for {t.Name} failed: {ex.Message}");
						// ignored
					}
				}

				throw new RuntimeError($"No matching constructor found for {t.FullName} with {(callArgs?.Length ?? 0)} args.");
			}

			var clrTypeObj = new CClrType(t, Factory);

			// Define the type in the current environment (use simple name for accessibility)
			env.Define(t.Name, clrTypeObj);

			// Expose static fields
			foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static)) {
				// Skip fields with generic parameters
				if (f.FieldType.ContainsGenericParameters) continue;

				try {
					var val = f.GetValue(null);
					if (val != null) env.Define(f.Name, new CClrObject(val));
				} catch {
					// Skip fields that can't be accessed
				}
			}

			// Expose static properties
			foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Static)) {
				// Skip properties with generic parameters
				if (p.PropertyType.ContainsGenericParameters) continue;

				if (p.GetMethod != null) {
					try {
						var val = p.GetValue(null);
						if (val != null) env.Define(p.Name, new CClrObject(val));
					} catch {
						// Skip properties that can't be accessed
					}
				}
			}
		}

		// Register all top-level exported types
		foreach (var t in asm.GetTypes()) {
			RegisterType(t);

			// Also register public nested types
			foreach (var nested in t.GetNestedTypes(BindingFlags.Public)) {
				RegisterType(nested);
			}
		}
	}
}

public static class EnumerableExtensions {
	public static IEnumerable<(int Index, T Item)> WithIndex<T>(this IEnumerable<T> source) {
		return source.Select((item, index) => (index, item));
	}
}