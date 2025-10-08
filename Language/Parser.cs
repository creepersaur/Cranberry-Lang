using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry;

public class Parser(string[] Tokens) {
	private int Pos = -1;
	private static readonly string[] CASTABLE = ["string", "number", "bool", "list", "dict", "char"];
	private static readonly string[] SHORTHANDS = ["+=", "-=", "*=", "/=", "^=", "%=", "++", "--"];
	private static readonly string[] FN_DECORATORS = ["extern"];

	// Get the token ahead (or by offset) as a string?
	public string? PeekAhead(int offset = 1) {
		int index = Pos + offset;
		return index >= Tokens.Length ? null : Tokens[index];
	}

	// Peek ahead and throw and error if token isn't expected
	private bool Expect(string token) {
		string? ahead = PeekAhead();
		if (ahead != token) {
			throw new ParseError($"Expected '{token}', got '{ahead switch {
				"\n" => "\\n",
				null => "[EOF]",
				_ => ahead
			}}'", Pos + 1);
		}

		return true;
	}

	// Check if token is equal to ahead (no error)
	public bool Check(string token, int offset = 1) {
		return PeekAhead(offset) == token;
	}

	// Advances and returns the token
	public string? Advance() {
		Pos += 1;
		return PeekAhead(0);
	}


	////////////////////////////////////////////////////////////
	// HANDLE STATEMENTS
	////////////////////////////////////////////////////////////
	public Node Parse() {
		SkipNewlines();
		string? token = PeekAhead();

		if (token == null)
			return new NullNode();

		if (token == "let") {
			return ParseLet();
		}

		if (token == "const") {
			return ParseLet(true);
		}

		if (token == "using") {
			return ParseUsingDirective();
		}

		if (token == "include") {
			return ParseInclude();
		}

		if (token == "namespace") {
			return ParseNamespace();
		}

		if (token == "fn") {
			return ParseFunctionDef();
		}

		if (token == "class") {
			return ParseClassDef();
		}

		if (token == "return") {
			Advance();
			if (Check(";") || Check("}") || Check(")") || PeekAhead() == null || Check("else") || IsNewline())
				return new ReturnNode(new NullNode());
			return new ReturnNode(ParseExpression());
		}

		if (token == "break") {
			Advance();
			if (Check(";") || Check("}") || Check(")") || PeekAhead() == null || Check("else") || IsNewline())
				return new BreakNode(new NullNode());
			return new BreakNode(ParseExpression());
		}

		if (token == "continue") {
			Advance();
			return new ContinueNode();
		}

		var is_identifier = IsIdentifier(token);

		if (is_identifier && (Check("=", 2) || Check(",", 2))) {
			return ParseAssignment();
		}

		if (is_identifier && SHORTHANDS.Contains(PeekAhead(2))) {
			return ParseShorthandAssignment();
		}

		return ParseExpression();
	}

	private IncludeDirective ParseInclude() {
		Advance();
		return new IncludeDirective(ParseExpression());
	}

	private NamespaceDirective ParseNamespace() {
		Advance();

		var spaces = new List<object>();
		string first = Advance()!;
		if (!IsIdentifier(first))
			throw new ParseError("Namespace names must always be Identifiers.", Pos);

		spaces.Add(first);

		while (Check("::")) {
			Advance();
			string name = Advance()!;
			if (!IsIdentifier(name))
				throw new ParseError("Namespace names must always be Identifiers.", Pos);

			spaces.Add(name);
		}

		if (Check("{") || Check("=>")) {
			return new NamespaceDirective(spaces.ToArray(), ParseBlock(should_out: false));
		}

		return new NamespaceDirective(spaces.ToArray());
	}

	private UsingDirective ParseUsingDirective() {
		Advance();

		var aliases = new Dictionary<string, string>();
		var wild_card = new List<string>();
		var spaces = new List<object>();

		string first = Advance()!;
		if (!IsIdentifier(first))
			throw new ParseError("Namespace names must always be Identifiers.", Pos);

		spaces.Add(first);

		if (Check("::") && PeekAhead(2) != "*") {
			while (Check("::")) {
				Advance();
				if (Check("{")) {
					Advance();
					var imports = new List<string>();

					while (!Check("}")) {
						string multi_name = Advance()!;
						
						if (!IsIdentifier(multi_name))
							throw new ParseError("Namespace names must always be Identifiers.", Pos);

						imports.Add(multi_name);

						if (PeekAhead() == "::" && PeekAhead(2) == "*") {
							wild_card.Add(multi_name);
							Advance();
							Advance();
						} else if (Check("as")) {
							Advance();
							string alias = Advance()!;
							if (!IsIdentifier(alias))
								throw new ParseError("Namespace aliases must always be identifiers.", Pos);

							if (!aliases.TryAdd(multi_name, alias))
								throw new ParseError("Cannot have duplicate namespace aliases.", Pos);
						}

						if (Check(",")) Advance();

						else break;
					}

					Expect("}");
					Advance();

					spaces.Add(imports.ToArray());
					break;
				}

				string name = Advance()!;
				if (!IsIdentifier(name) && name != "*")
					throw new ParseError("Namespace names must always be Identifiers.", Pos);

				spaces.Add(name);

				if (PeekAhead() == "::" && PeekAhead(2) == "*") {
					wild_card.Add(name);
					Advance();
					Advance();
				} else if (Check("as")) {
					Advance();

					string alias = Advance()!;
					if (!IsIdentifier(alias))
						throw new ParseError("Namespace aliases must always be Identifiers.", Pos);

					if (!aliases.TryAdd(name, alias))
						throw new ParseError("Cannot have duplicate namespace aliases.", Pos);
				}
			}
		} else {
			if (Check("::") && PeekAhead(2) == "*") {
				wild_card.Add(first);
				Advance();
				Advance();
			} else if (Check("as")) {
				Advance();

				string alias = Advance()!;
				if (!IsIdentifier(alias))
					throw new ParseError("Namespace aliases must always be Identifiers.", Pos);

				if (!aliases.TryAdd(first, alias))
					throw new ParseError("Cannot have duplicate namespace aliases.", Pos);
			}
		}

		return new UsingDirective(spaces.ToArray(), aliases, wild_card);
	}

	private ClassDef ParseClassDef() {
		Advance();

		var name = Advance()!;
		if (!IsIdentifier(name))
			throw new ParseError($"Expected Identifier for class name. Got `{name}`", Pos);

		FunctionNode? constructor = null;

		Expect("{");
		Advance();

		var funcs = new List<FunctionDef>();
		while (!Check("}")) {
			SkipNewlines();
			if (Check("constructor")) {
				if (constructor != null)
					throw new ParseError("Only one constructor can be present per Class", Pos + 1);

				Advance();
				constructor = ParseLambda();
			}

			if (Check("fn")) {
				funcs.Add(ParseFunctionDef());
			}

			if (Check(";")) Advance();
		}

		Expect("}");
		Advance();

		return new ClassDef(name, funcs.ToArray(), constructor);
	}

	private WhileNode ParseWhile(bool is_true = false) {
		Advance();

		return new WhileNode(is_true ? new BoolNode(true) : ParseExpression(), ParseBlock());
	}

	private ForNode ParseFOR() {
		Advance();

		string var_name = Advance()!;
		if (!IsIdentifier(var_name)) {
			throw new ParseError("For-loop variable name must be an identifier", Pos);
		}

		Expect("in");
		Advance();

		return new ForNode(var_name, ParseExpression(), ParseBlock());
	}

	private BlockNode ParseBlock(bool is_arrow = false, bool should_out = true) {
		SkipNewlines();
		if (Check("=>")) {
			Advance();
			var node = Parse();

			if (Check(";"))
				Advance();

			if (!should_out)
				return new BlockNode([node]);

			return new BlockNode([new OutNode(node)]);
		}

		if (is_arrow && !Check("{")) {
			var node = Parse();

			if (Check(";"))
				Advance();

			return new BlockNode([new OutNode(node)]);
		}

		Expect("{");
		Advance();
		SkipNewlines();

		var statements = new List<Node>();
		while (!Check("}")) {
			SkipNewlines();
			if (Check("}")) break;

			statements.Add(Parse());
			if (Check(";")) Advance();
		}

		Expect("}");
		Advance();

		return new BlockNode(statements.ToArray());
	}

	private FunctionDef ParseFunctionDef(bool requires_block = true) {
		Advance();

		var func_name = Advance();
		if (!IsIdentifier(func_name)) {
			throw new ParseError("Function name must be an identifier.", Pos);
		}

		Expect("(");
		Advance();

		var args = new List<string>();

		while (!Check(")")) {
			args.Add(Advance()!);
			if (Check(",")) {
				Advance();
			} else {
				break;
			}
		}

		Expect(")");
		Advance();

		BlockNode? block = null;
		if (requires_block)
			block = ParseBlock();

		return new FunctionDef(func_name!, args.ToArray(), block);
	}

	private IFNode ParseIF() {
		Advance();
		SkipNewlines();
		var condition = ParseExpression();
		SkipNewlines();
		var then = ParseBlock();
		SkipNewlines();

		var elif = new List<(Node, BlockNode)>();
		BlockNode? else_statement = null;

		while (Check("else")) {
			Advance();
			SkipNewlines();
			if (Check("if")) {
				Advance();
				SkipNewlines();
				var elif_condition = ParseExpression();
				SkipNewlines();
				elif.Add((elif_condition, ParseBlock()));
			} else {
				else_statement = ParseBlock();
				break;
			}
		}

		return new IFNode(condition, then, elif.ToArray(), else_statement);
	}

	private LetNode ParseLet(bool constant = false) {
		Advance();
		var names = new List<string>();
		var first_name = Advance();
		if (!IsIdentifier(first_name)) {
			throw new ParseError($"Expected identifier as variable name, got: `{first_name}`", Pos + 1);
		}

		names.Add(first_name!);

		while (Check(",")) {
			Advance();
			var name = Advance();
			if (!IsIdentifier(name)) {
				throw new ParseError($"Expected identifier as variable name, got: `{name}`", Pos + 1);
			}

			names.Add(name!);
		}

		// Initialization
		if (Check("=")) {
			Advance();

			var values = new List<Node>();
			values.Add(ParseExpression());

			while (Check(",")) {
				Advance();
				values.Add(ParseExpression());
			}

			if (values.Count != names.Count) {
				throw new ParseError($"Number of expressions ({values.Count}) should match number of variables ({names.Count}).", Pos);
			}

			return new LetNode(names.ToArray(), values.ToArray(), constant);
		}

		return new LetNode(names.ToArray(), Enumerable.Repeat(new NullNode(), names.Count).ToArray<Node>(), constant);
	}

	private SwitchNode ParseSwitch() {
		Advance();

		var expr = ParseExpression();
		var cases = new List<(Node[], BlockNode)>();
		BlockNode? default_case = null;

		Expect("{");
		Advance();

		while (!Check("}")) {
			SkipNewlines();

			if (Check("_")) {
				if (default_case != null)
					throw new RuntimeError("`switch` statements can have only 1 default case.");

				Advance();
				Expect("=>");
				Advance();

				default_case = ParseBlock(true);
				continue;
			}

			if (Check("}")) break;

			var new_cases = new List<Node>();
			while (!Check("=>")) {
				Console.WriteLine("Got a case");
				new_cases.Add(ParseExpression());

				if (Check(",")) Advance();
				else break;
			}

			SkipNewlines();

			Expect("=>");
			Advance();

			cases.Add((new_cases.ToArray(), ParseBlock(true)));
		}

		Expect("}");
		Advance();

		return new SwitchNode(expr, cases.ToArray(), default_case);
	}

	private AssignmentNode ParseAssignment() {
		var names = new List<string>();
		while (!Check("=")) {
			names.Add(Advance()!);

			if (Check(",")) Advance();
			else break;
		}
		
		Expect("=");
		Advance(); // consume `=`

		var values = new List<Node>();
		values.Add(ParseExpression());

		while (Check(",")) {
			Advance();
			values.Add(ParseExpression());
		}

		if (names.Count != values.Count)
			throw new ParseError($"Number of variables being assigned ({names.Count}) is not equal to number of values ({values.Count}).", Pos);
		
		return new AssignmentNode(names.ToArray(), values.ToArray());
	}

	private ShorthandAssignmentNode ParseShorthandAssignment() {
		string varName = Advance()!;
		string op = Advance()!; // consume `+=`

		Node? value = null;
		if (op != "++" && op != "--") {
			value = ParseExpression(); // right side
		}

		return new ShorthandAssignmentNode(varName, op, value);
	}


	////////////////////////////////////////////////////////////
	// HANDLE EXPRESSIONS
	////////////////////////////////////////////////////////////
	public Node ParseExpression() {
		SkipNewlines();
		return ParseLogical();
	}

	// Handles logical operators with lower precedence than comparisons
	public Node ParseLogical() {
		Node left = ParseComparison();

		while (Pos < Tokens.Length) {
			string? op = PeekAhead();
			if (op != "&&" && op != "||") break;

			Advance();
			Node right = ParseComparison(); // comparisons are next-highest precedence
			left = new BinaryOpNode(left, op, right);
		}

		return left;
	}

	public Node ParseComparison() {
		Node left = ParseAddSubtract(); // Next higher precedence

		while (Pos < Tokens.Length) {
			string? op = PeekAhead();
			if (op != "==" && op != "!=" && op != "<" && op != ">" && op != "<=" && op != ">=")
				break;

			Advance();
			Node right = ParseAddSubtract();
			left = new BinaryOpNode(left, op, right);
		}

		return left;
	}

	public Node ParseAddSubtract() {
		Node left = ParseTerm();

		while (Pos < Tokens.Length) {
			string? op = PeekAhead();
			if (op != "+" && op != "-") break;

			Advance();
			Node right = ParseTerm();
			left = new BinaryOpNode(left, op, right);
		}

		return left;
	}

	private Node ParseTerm() {
		Node left = ParseFallback();

		while (Pos < Tokens.Length) {
			string? op = PeekAhead();
			if (op != "*" && op != "/" && op != "%" && op != "//") break;

			Advance();
			Node right = ParseFallback();
			left = new BinaryOpNode(left, op, right);
		}

		return left;
	}

	private Node ParseFallback() {
		Node left = ParseRange();

		if (Check("??")) {
			Advance();

			return new FallbackNode(left, ParseExpression());
		}

		return left;
	}

	private Node ParseRange() {
		Node first = ParseUnary();

		if (Check("..")) {
			Advance();

			bool inclusive = Check("=");
			if (inclusive)
				Advance();

			Node second = ParseUnary();

			if (Check("..")) {
				Advance();

				Node third = ParseUnary();
				return new RangeNode(first, second, third, inclusive);
			}

			return new RangeNode(first, second, new NullNode(), inclusive);
		}

		return first;
	}

	private Node ParseUnary() {
		string? token = PeekAhead();

		if (token is "-" or "+" or "!" or "$") {
			Advance();
			return new UnaryOpNode(token, ParsePower());
		}

		return ParsePower();
	}

	private Node ParsePower() {
		Node left = ParseFactor();

		while (Pos < Tokens.Length) {
			string? op = PeekAhead();
			if (op != "^") break;

			Advance();
			Node right = ParseFactor();
			left = new BinaryOpNode(left, op, right);
		}

		return left;
	}

	private Node ParseFactor() {
		Node node = ParsePrimary(); // Parse the base expression first

		// Handle function calls (can be chained)
		while (true) {
			SkipNewlines();

			// IIFE
			if (Check("(")) {
				Advance(); // consume '('
				SkipNewlines();

				var args = new List<object>();

				// If next token is not ')', parse one or more arguments
				while (!Check(")")) {
					args.Add(ParseExpression());
					if (Check(",")) {
						Advance();
						SkipNewlines();
						if (Check(")")) break;
					} else {
						SkipNewlines();
						break;
					}
				}

				Expect(")"); // verify we have a closing ')'
				Advance(); // consume ')'

				// Build a call node targeting the previous node (left-associative)
				node = new FunctionCall("", args.ToArray()) {
					Target = node
				};

				continue;
			}

			if (Check(".")) {
				Advance(); // consume '.'
				var member = Advance()!; // implement or reuse method to get identifier token text
				if (!IsIdentifier(member)) {
					throw new ParseError("Tried to get member using non-identifier.", Pos);
				}

				if (Check("=")) {
					Advance();
					var value = ParseExpression();
					node = new MemberAssignmentNode(node, new StringNode(member), value);
				} else if (Check("+=") || Check("-=") || Check("*=") || Check("/=") || Check("^=") || Check("%=")) {
					var op = Advance()!;
					var value = ParseExpression();
					node = new MemberShorthandAssignmentNode(node, new StringNode(member), value, op);
				} else if (Check("++") || Check("--")) {
					var op = Advance()!;
					node = new MemberShorthandAssignmentNode(node, new StringNode(member), new NullNode(), op);
				} else {
					node = new MemberAccessNode(node, new StringNode(member));
				}

				continue;
			}

			if (Check("[")) {
				Advance(); // consume '.'
				var member = ParseExpression(); // implement or reuse method to get identifier token text
				Expect("]");
				Advance();

				if (Check("=")) {
					Advance();
					var value = ParseExpression();
					node = new MemberAssignmentNode(node, member, value);
				} else if (Check("+=") || Check("-=") || Check("*=") || Check("/=") || Check("^=") || Check("%=")) {
					var op = Advance()!;
					var value = ParseExpression();
					node = new MemberShorthandAssignmentNode(node, member, value, op);
				} else if (Check("++") || Check("--")) {
					var op = Advance()!;
					node = new MemberShorthandAssignmentNode(node, member, new NullNode(), op);
				} else {
					node = new MemberAccessNode(node, member);
				}

				continue;
			}

			break;
		}

		return node;
	}

	private DecoratorNode ParseDecorator() {
		string name = Advance()!;

		var args = new List<Node>();
		if (Check("(")) {
			Advance();
			SkipNewlines();
			while (!Check(")")) {
				SkipNewlines();
				args.Add(ParseExpression());

				if (Check(",")) Advance();
				else break;
			}

			Expect(")");
			Advance();
		}

		SkipNewlines();
		if (Check("fn") && FN_DECORATORS.Contains(name)) {
			Expect("fn");
			var func = ParseFunctionDef(false);
			return new DecoratorNode(name, args.ToArray(), func);
		}

		return new DecoratorNode(name, args.ToArray());
	}

	private ListNode ParseList() {
		Advance();

		var items = new List<Node>();

		while (!Check("]")) {
			items.Add(ParseExpression());
			if (Check(",")) {
				Advance();
			} else {
				break;
			}
		}

		Expect("]");
		Advance();

		return new ListNode(items);
	}

	private DictNode ParseDictionary() {
		Advance();

		var items = new Dictionary<Node, Node>();

		while (!Check("}")) {
			SkipNewlines();
			Node key = ParseExpression();

			SkipNewlines();
			Expect(":");
			Advance();
			SkipNewlines();

			Node value = ParseExpression();

			items.Add(key, value);

			if (Check(",")) {
				Advance();
			}
		}

		Expect("}");
		Advance();

		return new DictNode(items);
	}

	private Node ParsePrimary() {
		SkipNewlines();
		string? token = PeekAhead();

		// number literal
		if (double.TryParse(token, out double value)) {
			Advance();
			return new NumberNode(value);
		}

		switch (token) {
			case "true":
				Advance();
				return new BoolNode(true);
			case "false":
				Advance();
				return new BoolNode(false);
			case "nil":
				Advance();
				return new NullNode();
		}

		if (IsString(token)) {
			Advance();
			return new StringNode(token![1..^1]);
		}

		if (token == "while") {
			return ParseWhile();
		}

		if (token == "loop") {
			return ParseWhile(true);
		}

		if (token == "loop") {
			return ParseWhile(true);
		}

		if (token == "for") {
			return ParseFOR();
		}

		if (token == "@") {
			Advance();
			if (IsIdentifier(PeekAhead()))
				return ParseDecorator();

			SkipNewlines();
			Expect("{");
			return new ScopeNode(ParseBlock());
		}

		if (token == "{") {
			return ParseDictionary();
		}

		if (token == "if") {
			return ParseIF();
		}

		if (token == "out") {
			Advance();
			if (Check(";") || Check("}") || Check(")") || PeekAhead() == null || Check("else") || IsNewline())
				return new OutNode(new NullNode());
			return new OutNode(ParseExpression());
		}

		if (token == "switch") {
			return ParseSwitch();
		}

		if (token == "fn" && Advance() != null && SkipNewlines() && Expect("(")) {
			return ParseLambda();
		}

		if (token == "[") {
			return ParseList();
		}

		// variable (identifier)
		if (IsIdentifier(token)) {
			Advance();

			SkipNewlines();

			if (Check("(")) {
				Advance(); // Consume `(`

				if (CASTABLE.Contains(token)) {
					var to_cast = ParseExpression();
					Expect(")");
					Advance();

					return new CastNode(token!, to_cast);
				}

				var args = new List<object>();
				while (!Check(")")) {
					args.Add(ParseExpression());
					SkipNewlines();

					if (Check(",")) {
						Advance();
						SkipNewlines();
					} else {
						break;
					}
				}

				Expect(")"); // Consume `)`
				Advance();

				return new FunctionCall(token!, args.ToArray());
			}

			return new VariableNode(token!);
		}

		// parentheses
		if (token == "(") {
			Advance(); // Consume `(`

			Node expr = ParseExpression();

			Expect(")"); // Consume `)`
			Advance();
			return expr;
		}

		if (token == null)
			return new NullNode();

		throw new ParseError($"Unexpected token '{token switch {
			"\n" => "\\n",
			_ => token
		}}' after `{PeekAhead(0) switch {
		"\n" => "\\n",
			_ => PeekAhead(0)
		}}`", Pos + 1);
	}

	private FunctionNode ParseLambda() {
		Expect("(");
		Advance();

		var args = new List<string>();
		while (!Check(")")) {
			SkipNewlines();
			string? arg = PeekAhead();
			if (!IsIdentifier(arg)) {
				throw new ParseError($"Expected parameter name, got '{arg}'", Pos);
			}

			args.Add(Advance()!);
			SkipNewlines();

			if (Check(",")) {
				Advance();
				SkipNewlines();
			} else {
				break;
			}
		}

		Expect(")");
		Advance();

		return new FunctionNode(args.ToArray(), ParseBlock());
	}

	private bool IsNewline() => PeekAhead() == "\n";

	private bool SkipNewlines() {
		while (PeekAhead() != null && IsNewline()) {
			Advance();
		}

		return true;
	}


	private static bool IsIdentifier(string? token) {
		return token != null &&
		       (char.IsLetter(token[0]) || token[0] == '_') &&
		       token.All(c => char.IsLetterOrDigit(c) || c == '_');
	}

	private static bool IsString(string? token) {
		var Quotes = Lexer.QUOTES;
		return token != null && Quotes.Contains(token[0]) && Quotes.Contains(token[^1]);
	}
}