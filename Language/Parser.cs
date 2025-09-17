using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry;

public class Parser(string[] Tokens) {
	private int Pos = -1;
	private static readonly string[] CASTABLE = ["string", "number", "bool", "list", "dict"];
	private static readonly string[] SHORTHANDS = ["+=", "-=", "*=", "/=", "^=", "%=", "++", "--"];

	// Get the token ahead (or by offset) as a string?
	public string? PeekAhead(int offset = 1) {
		int index = Pos + offset;
		return index >= Tokens.Length ? null : Tokens[index];
	}

	// Peek ahead and throw and error if token isn't expected
	private void Expect(string token) {
		string? ahead = PeekAhead();
		if (ahead != token) {
			throw new ParseError($"Expected '{token}', got '{ahead}'", Pos + 1);
		}
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
		string? token = PeekAhead();

		if (token == "let") {
			return ParseLet();
		}

		if (token == "using") {
			return ParseUsingDirective();
		}

		if (token == "fn") {
			return ParseFunctionDef();
		}

		if (token == "class") {
			return ParseClassDef();
		}

		if (token == "return") {
			Advance();
			if (Check(";") || Check("}") || Check(")") || PeekAhead() == null || Check("else"))
				return new ReturnNode(new NullNode());
			return new ReturnNode(ParseExpression());
		}

		if (token == "break") {
			Advance();
			if (Check(";") || Check("}") || Check(")") || PeekAhead() == null || Check("else"))
				return new BreakNode(new NullNode());
			return new BreakNode(ParseExpression());
		}

		if (token == "continue") {
			Advance();
			return new ContinueNode();
		}

		var is_identifier = IsIdentifier(token);

		if (is_identifier && Check("=", 2)) {
			return ParseAssignment();
		}

		if (is_identifier && SHORTHANDS.Contains(PeekAhead(2))) {
			return ParseShorthandAssignment();
		}

		return ParseExpression();
	}

	private UsingDirective ParseUsingDirective() {
		Advance();

		var spaces = new List<object>();
		string first = Advance()!;
		if (!IsIdentifier(first))
			throw new ParseError("Namespace names must always be Identifiers.", Pos);
		
		spaces.Add(first);

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

					if (Check(",")) Advance();
					else break;
				}
				
				Expect("}");
				Advance();
				
				spaces.Add(imports.ToArray());
				break;
			}
			
			string name = Advance()!;
			if (!IsIdentifier(name))
				throw new ParseError("Namespace names must always be Identifiers.", Pos);
			
			spaces.Add(name);
		}

		return new UsingDirective(spaces.ToArray());
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
			if (Check("constructor")) {
				if (constructor != null)
					throw new ParseError("Only one constructor can be present per Class", Pos + 1);
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

	private WhileNode ParseWhile() {
		Advance();

		return new WhileNode(ParseExpression(), ParseBlock());
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

	private BlockNode ParseBlock() {
		if (Check("=>")) {
			Advance();
			var node = Parse();

			if (Check(";"))
				Advance();

			return new BlockNode([new OutNode(node)]);
		}

		Expect("{");
		Advance();

		var statements = new List<Node>();
		while (!Check("}")) {
			statements.Add(Parse());
			if (Check(";"))
				Advance();
		}

		Expect("}");
		Advance();

		return new BlockNode(statements.ToArray());
	}

	private FunctionDef ParseFunctionDef() {
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

		return new FunctionDef(func_name!, args.ToArray(), ParseBlock());
	}

	private IFNode ParseIF() {
		Advance();
		var condition = ParseExpression();
		var then = ParseBlock();

		var elif = new List<(Node, BlockNode)>();
		BlockNode? else_statement = null;

		while (Check("else")) {
			Advance();
			if (Check("if")) {
				Advance();
				var elif_condition = ParseExpression();
				elif.Add((elif_condition, ParseBlock()));
			} else {
				else_statement = ParseBlock();
				break;
			}
		}

		return new IFNode(condition, then, elif.ToArray(), else_statement);
	}

	private LetNode ParseLet() {
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

			return new LetNode(names.ToArray(), values.ToArray());
		}

		return new LetNode(names.ToArray(), Enumerable.Repeat(new NullNode(), names.Count).ToArray<Node>());
	}

	private SwitchNode ParseSwitch() {
		Advance();

		var expr = ParseExpression();
		var cases = new List<(Node[], BlockNode)>();
		BlockNode? default_case = null;

		Expect("{");
		Advance();

		while (!Check("}")) {
			if (default_case == null && Check("_")) {
				Advance();
				Expect(":");
				Advance();

				default_case = ParseBlock();
			}

			if (Check("}")) break;

			var new_cases = new List<Node>();
			while (!Check(":")) {
				new_cases.Add(ParseExpression());
				
				if (Check(",")) Advance();
				else break;
			}
			
			Expect(":");
			Advance();

			cases.Add((new_cases.ToArray(), ParseBlock()));
		}

		Expect("}");
		Advance();

		return new SwitchNode(expr, cases.ToArray(), default_case);
	}

	private AssignmentNode ParseAssignment() {
		string varName = Advance()!;
		Advance(); // consume `=`

		Node value = ParseExpression(); // right side
		return new AssignmentNode(varName, value);
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
		return ParseComparison();
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

			return new FallbackNode(left,ParseExpression());
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

		if (token is "-" or "+" or "!") {
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
			// IIFE
			if (Check("(")) {
				Advance(); // consume '('

				var args = new List<Node>();

				// If next token is not ')', parse one or more arguments
				if (!Check(")")) {
					while (true) {
						args.Add(ParseExpression());

						if (Check(",")) {
							Advance(); // consume comma and keep parsing args
							// allow trailing comma before ')', so if next is ')' the loop will break
							if (Check(")")) break;
						} else {
							break;
						}
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
				} else {
					node = new MemberAccessNode(node, member);
				}
				continue;
			}

			break;
		}

		return node;
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
			Node key;
			
			string key_id = PeekAhead()!;
			if (IsIdentifier(key_id)) {
				key = new StringNode(key_id);
				Advance();
			} else {
				key = ParseExpression();
			}
			
			Expect(":");
			Advance();

			Node value = ParseExpression();
			
			items.Add(key, value);
			
			if (Check(",")) {
				Advance();
			} else break;
		}
		
		Expect("}");
		Advance();

		return new DictNode(items);
	}

	private Node ParsePrimary() {
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

		if (token == "for") {
			return ParseFOR();
		}

		if (token == "@") {
			Advance();
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
			if (Check(";") || Check("}") || Check(")") || PeekAhead() == null || Check("else"))
				return new OutNode(new NullNode());
			return new OutNode(ParseExpression());
		}

		if (token == "switch") {
			return ParseSwitch();
		}

		if (token == "fn" && Check("(", 2)) {
			return ParseLambda();
		}

		if (token == "[") {
			return ParseList();
		}

		// variable (identifier)
		if (IsIdentifier(token)) {
			Advance();

			if (Check("(")) {
				Advance(); // Consume `(`
				
				if (CASTABLE.Contains(token)) {
					var to_cast = ParseExpression();
					Expect(")");
					Advance();

					return new CastNode(token!, to_cast);
				}

				var args = new List<Node>();
				while (!Check(")")) {
					args.Add(ParseExpression());

					if (Check(",")) {
						Advance();
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

		throw new ParseError($"Unexpected token '{token}'", Pos + 1);
	}

	private FunctionNode ParseLambda() {
		Advance(); // consume 'fn'

		Expect("(");
		Advance();

		var args = new List<string>();
		while (!Check(")")) {
			string? arg = PeekAhead();
			if (!IsIdentifier(arg)) {
				throw new ParseError($"Expected parameter name, got '{arg}'", Pos);
			}

			args.Add(Advance()!);

			if (Check(",")) {
				Advance();
			} else {
				break;
			}
		}

		Expect(")");
		Advance();

		return new FunctionNode(args.ToArray(), ParseBlock());
	}

	private static bool IsIdentifier(string? token) {
		return token != null &&
			char.IsLetter(token[0]) || token![0] == '_' &&
			token.All(c => char.IsLetterOrDigit(c) || c == '_');
	}

	private static bool IsString(string? token) {
		var Quotes = "'\"".ToCharArray();
		return token != null && Quotes.Contains(token[0]) && Quotes.Contains(token[^1]);
	}
}