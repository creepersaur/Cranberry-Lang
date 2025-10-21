using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry {
	public class Parser(Token[] tokens, FileInfo file_info) {
		private int Pos = -1;

		private static readonly string[] CASTABLE = ["string", "number", "bool", "list", "dict", "char"];
		private static readonly string[] SHORTHANDS = ["+=", "-=", "*=", "/=", "^=", "%=", "++", "--"];
		private static readonly string[] FN_DECORATORS = ["extern"];

		// Get the token ahead (or by offset)
		// NOTE: this preserves your original semantics where PeekAhead() with default offset=1
		// refers to the 'current' token when Pos == -1 at start.
		public Token? PeekAhead(int offset = 1) {
			int index = Pos + offset;
			return (index >= 0 && index < tokens.Length) ? tokens[index] : null;
		}

		private static string TokenToDisplay(Token? tok) {
			if (tok == null) return "[EOF]";
			var v = tok.Value;
			if (v == "\n") return "\\n";
			return v;
		}

		// Peek ahead and throw an error if token isn't expected
		private void Expect(string token) {
			Token? ahead = PeekAhead();
			var got = TokenToDisplay(ahead);
			if (got != token) {
				throw new ParseError(
					$"Expected '{token}', got '{got}'",
					ahead ?? new Token("[EOF]", tokens[Pos - 1].Line, tokens[Pos - 1].Col + 2, tokens[Pos - 1].FileName, tokens[Pos - 1].FilePath)
				);
			}
		}

		// Check if token is equal to ahead (no error)
		public bool Check(string token, int offset = 1) {
			Token? t = PeekAhead(offset);
			return t != null && t.Value == token;
		}

		// Advances and returns the token
		public Token? Advance() {
			Pos += 1;
			return PeekAhead(0);
		}


		////////////////////////////////////////////////////////////
		// HANDLE STATEMENTS
		////////////////////////////////////////////////////////////
		public Node Parse() {
			SkipNewlines();
			Token? start_token = PeekAhead();
			if (start_token == null)
				return new NullNode(tokens.Last());

			string token = start_token.Value;

			if (token == "using") {
				return ParseUsingDirective();
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
					return new ReturnNode(start_token, new NullNode(start_token));
				return new ReturnNode(start_token, ParseExpression());
			}

			if (token == "break") {
				Advance();
				if (Check(";") || Check("}") || Check(")") || PeekAhead() == null || Check("else") || IsNewline())
					return new BreakNode(start_token, new NullNode(start_token));
				return new BreakNode(start_token, ParseExpression());
			}

			if (token == "continue") {
				Advance();
				return new ContinueNode(start_token);
			}

			var isIdentifier = IsIdentifier(PeekAhead());

			if (isIdentifier && (Check("=", 2) || Check(",", 2))) {
				return ParseAssignment();
			}

			return ParseExpression();
		}

		private NamespaceDirective ParseNamespace() {
			Token start_token = PeekAhead()!;
			Advance();

			var spaces = new List<object>();
			var first = Advance();
			if (!IsIdentifier(first))
				throw new ParseError("Namespace names must always be Identifiers.", first!);

			spaces.Add(first!.Value);

			while (Check("::")) {
				Advance();
				var name = Advance();
				if (!IsIdentifier(name))
					throw new ParseError("Namespace names must always be Identifiers.", name!);

				spaces.Add(name!.Value);
			}

			if (Check("{") || Check("=>")) {
				return new NamespaceDirective(start_token, spaces.ToArray(), ParseBlock(should_out: false));
			}

			return new NamespaceDirective(start_token, spaces.ToArray());
		}

		private UsingDirective ParseUsingDirective() {
			Token start_token = PeekAhead()!;
			Advance();

			var aliases = new Dictionary<string, string>();
			var wild_card = new List<string>();
			var spaces = new List<object>();

			var first = Advance();
			if (!IsIdentifier(first))
				throw new ParseError("Namespace names must always be Identifiers.", first!);

			spaces.Add(first!.Value);

			if (Check("::") && PeekAhead(2)?.Value != "*") {
				while (Check("::")) {
					Advance();
					if (Check("{")) {
						Advance();
						var imports = new List<string>();

						while (!Check("}")) {
							var multiName = Advance();
							if (!IsIdentifier(multiName))
								throw new ParseError("Namespace names must always be Identifiers.", multiName!);

							imports.Add(multiName!.Value);

							if (PeekAhead()?.Value == "::" && PeekAhead(2)?.Value == "*") {
								wild_card.Add(multiName.Value);
								Advance();
								Advance();
							} else if (Check("as")) {
								Advance();
								var alias = Advance();
								if (!IsIdentifier(alias))
									throw new ParseError("Namespace aliases must always be identifiers.", alias!);

								if (!aliases.TryAdd(multiName.Value, alias!.Value))
									throw new ParseError("Cannot have duplicate namespace aliases.", alias);
							}

							if (Check(",")) Advance();
							else break;
						}

						Expect("}");
						Advance();

						spaces.Add(imports.ToArray());
						break;
					}

					var name = Advance();
					if (!IsIdentifier(name) && name?.Value != "*")
						throw new ParseError("Namespace names must always be Identifiers.", name!);

					spaces.Add(name!.Value);

					if (PeekAhead()?.Value == "::" && PeekAhead(2)?.Value == "*") {
						wild_card.Add(name.Value);
						Advance();
						Advance();
					} else if (Check("as")) {
						Advance();

						var alias = Advance();
						if (!IsIdentifier(alias))
							throw new ParseError("Namespace aliases must always be Identifiers.", alias!);

						if (!aliases.TryAdd(name.Value, alias!.Value))
							throw new ParseError("Cannot have duplicate namespace aliases.", alias);
					}
				}
			} else {
				if (Check("::") && PeekAhead(2)?.Value == "*") {
					wild_card.Add(first.Value);
					Advance();
					Advance();
				} else if (Check("as")) {
					Advance();

					var alias = Advance();
					if (!IsIdentifier(alias))
						throw new ParseError("Namespace aliases must always be Identifiers.", alias!);

					if (!aliases.TryAdd(first.Value, alias!.Value))
						throw new ParseError("Cannot have duplicate namespace aliases.", alias);
				}
			}

			return new UsingDirective(start_token, spaces.ToArray(), aliases, wild_card);
		}

		private ClassDef ParseClassDef() {
			Token start_token = PeekAhead()!;
			Advance();

			var nameToken = Advance();
			if (!IsIdentifier(nameToken))
				throw new ParseError($"Expected Identifier for class name. Got `{TokenToDisplay(nameToken)}`", nameToken!);

			FunctionNode? constructor = null;

			Expect("{");
			Advance();

			var funcs = new List<FunctionDef>();
			var lets = new List<LetNode>();

			while (!Check("}")) {
				SkipNewlines();
				if (Check("constructor")) {
					if (constructor != null)
						throw new ParseError("Only one constructor can be present per Class", PeekAhead()!);

					Advance();
					constructor = ParseLambda();
				} else if (Check("fn")) funcs.Add(ParseFunctionDef());
				else if (Check("let")) lets.Add(ParseLet());
				else if (Check(";")) Advance();
				else if (Check("}")) break;
				else throw new ParseError($"Class definitions only support functions, `let` statements and a constructor. Got `{TokenToDisplay(PeekAhead())}`", PeekAhead()!);
			}

			Expect("}");
			Advance();

			return new ClassDef(start_token, nameToken!.Value, funcs.ToArray(), lets.ToArray(), constructor);
		}

		private WhileNode ParseWhile(bool is_true = false) {
			Token start_token = PeekAhead()!;
			Advance();
			return new WhileNode(start_token, is_true ? new BoolNode(start_token, true) : ParseExpression(), ParseBlock());
		}

		private ForNode ParseFOR() {
			Token start_token = PeekAhead()!;
			Advance();

			var varNameToken = Advance();
			if (!IsIdentifier(varNameToken)) {
				throw new ParseError("For-loop variable name must be an identifier", varNameToken!);
			}

			Expect("in");
			Advance();

			return new ForNode(start_token, varNameToken!.Value, ParseExpression(), ParseBlock());
		}

		private BlockNode ParseBlock(bool is_arrow = false, bool should_out = true) {
			Token start_token = PeekAhead()!;
			SkipNewlines();
			if (Check("=>")) {
				Advance();
				var node = Parse();

				if (Check(";"))
					Advance();

				if (!should_out)
					return new BlockNode(start_token, [node]);

				return new BlockNode(start_token, [new OutNode(start_token, node)]);
			}

			if (is_arrow && !Check("{")) {
				var node = Parse();

				if (Check(";")) Advance();

				return new BlockNode(start_token, [new OutNode(start_token, node)]);
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

			return new BlockNode(start_token, statements.ToArray());
		}

		private FunctionDef ParseFunctionDef(bool requires_block = true) {
			Token start_token = PeekAhead()!;
			Advance();
			SkipNewlines();

			var funcNameToken = Advance();
			if (!IsIdentifier(funcNameToken)) {
				throw new ParseError("Function name must be an identifier.", funcNameToken!);
			}

			SkipNewlines();

			Expect("(");
			Advance();

			SkipNewlines();
			var args = new List<string>();

			while (!Check(")")) {
				var a = Advance();
				if (a == null) throw new ParseError("Unexpected EOF while parsing parameters.", a!);
				args.Add(a.Value);
				SkipNewlines();
				if (Check(",")) {
					Advance();
					SkipNewlines();
				} else break;
			}

			Expect(")");
			Advance();
			SkipNewlines();

			BlockNode? block = null;
			if (requires_block)
				block = ParseBlock();

			return new FunctionDef(start_token, funcNameToken!.Value, args.ToArray(), block);
		}

		private IFNode ParseIF() {
			Token start_token = PeekAhead()!;
			Advance();
			SkipNewlines();
			var condition = ParseExpression();
			SkipNewlines();
			var then = ParseBlock();
			SkipNewlines();

			var elif = new List<(Node, BlockNode)>();
			BlockNode? elseStatement = null;

			while (Check("else")) {
				Advance();
				SkipNewlines();
				if (Check("if")) {
					Advance();
					SkipNewlines();
					var elifCondition = ParseExpression();
					SkipNewlines();
					elif.Add((elifCondition, ParseBlock()));
					SkipNewlines();
				} else {
					elseStatement = ParseBlock();
					break;
				}
			}

			return new IFNode(start_token, condition, then, elif.ToArray(), elseStatement);
		}

		private LetNode ParseLet(bool constant = false) {
			var start_token = PeekAhead()!;
			
			Advance();
			var names = new List<object>();
			var firstName = Advance();
			if (firstName is { Value: "(" }) {
				var destructured = new List<string>();

				if (Check(")")) throw new ParseError("Empty destructuring is not allowed.", firstName);

				while (!Check(")")) {
					var dName = Advance();
					if (dName is { Value: "(" }) throw new ParseError("Cannot destructure again inside destructure syntax.", dName);
					if (!IsIdentifier(dName)) {
						throw new ParseError($"Expected identifier as destructured name, got: `{TokenToDisplay(dName)}`", dName!);
					}

					destructured.Add(dName!.Value);

					if (PeekAhead()?.Value == ",") Advance();
					else break;
				}

				Expect(")");
				Advance();

				names.Add(destructured);
			} else if (!IsIdentifier(firstName)) {
				throw new ParseError($"Expected identifier as variable name, got: `{TokenToDisplay(firstName)}`", firstName!);
			} else {
				names.Add(firstName!.Value);
			}

			while (Check(",")) {
				Advance();
				var name = Advance();

				if (name is { Value: "(" }) {
					var destructured = new List<string>();

					while (!Check(")")) {
						var dName = Advance();
						if (dName is { Value: "(" }) throw new ParseError("Cannot destructure again inside destructure syntax.", dName);
						if (!IsIdentifier(dName)) {
							throw new ParseError($"Expected identifier as destructured name, got: `{TokenToDisplay(dName)}`", dName!);
						}

						destructured.Add(dName!.Value);
					}

					Expect(")");
					Advance();

					names.Add(destructured);
				} else if (!IsIdentifier(name)) {
					throw new ParseError($"Expected identifier as variable name or destructuring, got: `{TokenToDisplay(name)}`", name!);
				} else {
					names.Add(name!.Value);
				}
			}

			// Initialization
			if (Check("=")) {
				var equals = Advance();
				SkipNewlines();
				
				if (PeekAhead() == null) {
					throw new ParseError("Expected value after `=`.", equals);
				}

				var values = new List<Node>();
				values.Add(ParseExpression());

				while (Check(",")) {
					Advance();
					values.Add(ParseExpression());
				}

				return new LetNode(start_token, names.ToArray(), values.ToArray(), constant);
			}

			return new LetNode(start_token, names.ToArray(), Enumerable.Repeat(new NullNode(start_token), names.Count).ToArray<Node>(), constant);
		}

		private SwitchNode ParseSwitch() {
			Token start_token = PeekAhead()!;
			Advance();

			var expr = ParseExpression();
			var cases = new List<(Node[], BlockNode)>();
			BlockNode? defaultCase = null;

			Expect("{");
			Advance();

			while (!Check("}")) {
				SkipNewlines();

				if (Check("_")) {
					if (defaultCase != null)
						throw new RuntimeError("`switch` statements can have only 1 default case.");

					Advance();
					Expect("=>");
					Advance();

					defaultCase = ParseBlock(true);
					continue;
				}

				if (Check("}")) break;

				var newCases = new List<Node>();
				while (!Check("=>")) {
					newCases.Add(ParseExpression());

					if (Check(",")) Advance();
					else break;
				}

				SkipNewlines();

				Expect("=>");
				Advance();

				cases.Add((newCases.ToArray(), ParseBlock(true)));
			}

			Expect("}");
			Advance();

			return new SwitchNode(start_token, expr, cases.ToArray(), defaultCase);
		}

		private AssignmentNode ParseAssignment() {
			Token start_token = PeekAhead()!;
			
			var names = new List<string>();
			while (!Check("=")) {
				var n = Advance();
				if (n == null) throw new ParseError("Unexpected EOF in assignment target.", n!);
				names.Add(n.Value);

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
				throw new ParseError($"Number of variables being assigned ({names.Count}) is not equal to number of values ({values.Count}).", PeekAhead()!);

			return new AssignmentNode(start_token, names.ToArray(), values.ToArray());
		}

		private ShorthandAssignmentNode ParseShorthandAssignment() {
			Token start_token = PeekAhead()!;
			var varTok = Advance();
			var opTok = Advance();
			if (varTok == null || opTok == null) throw new ParseError("Unexpected EOF in shorthand assignment.", PeekAhead()!);

			string varName = varTok.Value;
			string op = opTok.Value; // consume `+=`

			Node? value = null;
			if (op != "++" && op != "--") {
				value = ParseExpression(); // right side
			}

			return new ShorthandAssignmentNode(start_token, varName, op, value);
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
			Token start_token = PeekAhead()!;
			Node left = ParseComparison();

			while (PeekAhead() != null) {
				var opTok = PeekAhead();
				if (opTok == null) break;
				string op = opTok.Value;
				if (op != "&&" && op != "||") break;

				Advance();
				Node right = ParseComparison(); // comparisons are next-highest precedence
				left = new BinaryOpNode(start_token, left, op, right);
			}

			return left;
		}

		public Node ParseComparison() {
			Token start_token = PeekAhead()!;
			Node left = ParseRange(); // Next higher precedence

			while (PeekAhead() != null) {
				var opTok = PeekAhead();
				if (opTok == null) break;
				string op = opTok.Value;
				if (op != "==" && op != "!=" && op != "<" && op != ">" && op != "<=" && op != ">=")
					break;

				Advance();
				Node right = ParseAddSubtract();
				left = new BinaryOpNode(start_token, left, op, right);
			}

			return left;
		}

		private Node ParseRange() {
			Token start_token = PeekAhead()!;
			Node first = ParseAddSubtract();

			if (Check("..")) {
				Advance();

				bool inclusive = Check("=");
				if (inclusive) Advance();

				Node second = ParseAddSubtract();

				if (Check("..")) {
					Advance();
					Node third = ParseAddSubtract();
					return new RangeNode(start_token, first, second, third, inclusive);
				}

				return new RangeNode(start_token, first, second, new NullNode(start_token), inclusive);
			}

			return first;
		}

		public Node ParseAddSubtract() {
			Token start_token = PeekAhead()!;
			Node left = ParseTerm();

			while (PeekAhead() != null) {
				var opTok = PeekAhead();
				if (opTok == null) break;
				string op = opTok.Value;
				if (op != "+" && op != "-") break;

				Advance();
				Node right = ParseTerm();
				left = new BinaryOpNode(start_token, left, op, right);
			}

			return left;
		}

		private Node ParseTerm() {
			Token start_token = PeekAhead()!;
			Node left = ParseFallback();

			while (PeekAhead() != null) {
				var opTok = PeekAhead();
				if (opTok == null) break;
				string op = opTok.Value;
				if (op != "*" && op != "/" && op != "%" && op != "//") break;

				Advance();
				Node right = ParseFallback();
				left = new BinaryOpNode(start_token, left, op, right);
			}

			return left;
		}

		private Node ParseFallback() {
			Token start_token = PeekAhead()!;
			Node left = ParseUnary();

			if (Check("??")) {
				Advance();
				return new FallbackNode(start_token, left, ParseExpression());
			}

			return left;
		}

		private Node ParseUnary() {
			Token start_token = PeekAhead()!;
			var tok = PeekAhead();
			string token = tok?.Value ?? "";

			if (token is "-" or "+" or "!" or "$") {
				Advance();

				return new UnaryOpNode(start_token, token, ParseUnary());
			}

			return ParsePower();
		}

		private Node ParsePower() {
			Token start_token = PeekAhead()!;
			Node left = ParseFactor();

			while (PeekAhead() != null) {
				var opTok = PeekAhead();
				if (opTok == null) break;
				string op = opTok.Value;
				if (op != "^") break;

				Advance();
				Node right = ParseFactor();
				left = new BinaryOpNode(start_token, left, op, right);
			}

			return left;
		}

		private Node ParseFactor() {
			Token start_token = PeekAhead()!;
			Node node = ParsePrimary(); // Parse the base expression first

			// Handle function calls (can be chained)
			while (true) {
				SkipNewlines();

				// IIFE / call
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
					node = new FunctionCall(start_token, "", args.ToArray()) { Target = node };
					continue;
				}

				// member access .foo
				if (Check(".")) {
					Advance(); // consume '.'
					var memberTok = Advance()!;
					if (!IsIdentifier(memberTok)) {
						throw new ParseError("Tried to get member using non-identifier.", memberTok);
					}

					if (Check("=")) {
						Advance();
						var value = ParseExpression();
						node = new MemberAssignmentNode(memberTok, node, new StringNode(memberTok, memberTok.Value), value);
					} else if (Check("+=") || Check("-=") || Check("*=") || Check("/=") || Check("^=") || Check("%=")) {
						var opTok = Advance()!;
						var value = ParseExpression();
						node = new MemberShorthandAssignmentNode(memberTok, node, new StringNode(memberTok, memberTok.Value), value, opTok.Value);
					} else if (Check("++") || Check("--")) {
						var opTok = Advance()!;
						node = new MemberShorthandAssignmentNode(memberTok, node, new StringNode(memberTok, memberTok.Value), new NullNode(start_token), opTok.Value);
					} else {
						node = new MemberAccessNode(memberTok, node, new StringNode(memberTok, memberTok.Value));
					}

					continue;
				}

				// index access [expr]
				if (Check("[")) {
					Advance(); // consume '['
					var member = ParseExpression();
					Expect("]");
					Advance();

					if (Check("=")) {
						Advance();
						var value = ParseExpression();
						node = new MemberAssignmentNode(start_token, node, member, value);
					} else if (Check("+=") || Check("-=") || Check("*=") || Check("/=") || Check("^=") || Check("%=")) {
						var opTok = Advance()!;
						var value = ParseExpression();
						node = new MemberShorthandAssignmentNode(start_token, node, member, value, opTok.Value);
					} else if (Check("++") || Check("--")) {
						var opTok = Advance()!;
						node = new MemberShorthandAssignmentNode(start_token, node, member, new NullNode(start_token), opTok.Value);
					} else {
						node = new MemberAccessNode(start_token, node, member);
					}

					continue;
				}

				break;
			}

			return node;
		}

		private DecoratorNode ParseDecorator() {
			Token start_token = PeekAhead()!;
			var nameTok = Advance();
			if (nameTok == null) throw new ParseError("Unexpected EOF after @", nameTok!);
			string name = nameTok.Value;
			SkipNewlines();

			var args = new List<Node>();
			if (Check("(")) {
				Advance();
				SkipNewlines();
				while (!Check(")")) {
					SkipNewlines();
					args.Add(ParseExpression());
					SkipNewlines();

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
				return new DecoratorNode(start_token, name, args.ToArray(), func);
			}

			return new DecoratorNode(start_token, name, args.ToArray());
		}

		private ListNode ParseList() {
			Token start_token = PeekAhead()!;
			Advance();

			var items = new List<Node>();

			while (!Check("]")) {
				items.Add(ParseExpression());
				if (Check(",")) Advance();
				else break;
			}

			Expect("]");
			Advance();

			return new ListNode(start_token, items);
		}

		private DictNode ParseDictionary() {
			Token start_token = PeekAhead()!;
			Advance();
			SkipNewlines();

			var items = new Dictionary<Node, Node>();

			while (!Check("}")) {
				SkipNewlines();
				Node key = ParseExpression();

				SkipNewlines();

				if (Check("=")) {
					if (key is not VariableNode v)
						throw new ParseError("`=` can only be used for identifier named keys in dictionaries.", PeekAhead());
					key = new StringNode(start_token, v.Name);
				} else Expect(":");

				Advance();
				SkipNewlines();

				Node value = ParseExpression();

				items.Add(key, value);

				if (Check(",")) Advance();
			}

			Expect("}");
			Advance();

			return new DictNode(start_token, items);
		}

		private Node ParsePrimary() {
			SkipNewlines();
			Token start_token = PeekAhead()!;
			var tokenTok = PeekAhead();
			if (tokenTok == null) {
				var lines = File.ReadLines(file_info.FullName).ToArray();	
				throw new ParseError(
					"Unexpected end of file [EOF]. Expected more tokens.",
					new Token(
						"[EOF]",
						lines.Length, 
						lines.Last().Length, 
						file_info.Name, 
						file_info.FullName
					)
				);
			}
			
			string token = tokenTok.Value;

			// number literal
			if (double.TryParse(token, out double value)) {
				Advance();
				return new NumberNode(start_token, value);
			}

			switch (token) {
				case "true":
					Advance();
					return new BoolNode(start_token, true);
				case "false":
					Advance();
					return new BoolNode(start_token, false);
				case "nil":
					Advance();
					return new NullNode(start_token);
			}

			if (IsString(tokenTok.Value)) {
				Advance();
				var raw = tokenTok.Value;
				return new StringNode(start_token, raw[1..^1]);
			}

			if (token == "let") return ParseLet();
			if (token == "const") return ParseLet(true);
			if (token == "while") return ParseWhile();
			if (token == "loop") return ParseWhile(true);
			if (token == "for") return ParseFOR();

			if (token == "@") {
				Advance();
				if (IsIdentifier(PeekAhead())) return ParseDecorator();

				SkipNewlines();
				Expect("{");
				return new ScopeNode(start_token, ParseBlock());
			}

			if (token == "{") return ParseDictionary();
			if (token == "if") return ParseIF();

			if (token == "out") {
				Advance();
				if (Check(";") || Check("}") || Check(")") || PeekAhead() == null || Check("else") || IsNewline())
					return new OutNode(start_token, new NullNode(start_token));
				return new OutNode(start_token, ParseExpression());
			}

			if (token == "switch") return ParseSwitch();

			// inline lambda: fn (...) { ... }
			if (token == "fn") {
				// consume 'fn' and parse lambda
				Advance();
				return ParseLambda();
			}

			if (token == "[") return ParseList();

			// variable (identifier)
			if (IsIdentifier(PeekAhead())) {
				// shorthand check looks two tokens ahead (token positions preserved)
				if (PeekAhead(2) != null && SHORTHANDS.Contains(PeekAhead(2)!.Value)) {
					return ParseShorthandAssignment();
				}

				var varTok = Advance();
				if (varTok == null) return new NullNode(start_token);
				var varName = varTok.Value;

				SkipNewlines();

				if (Check("(")) {
					Advance(); // Consume `(`

					if (CASTABLE.Contains(varName)) {
						var toCast = ParseExpression();
						Expect(")");
						Advance();

						return new CastNode(start_token, varName, toCast);
					}

					var args = new List<object>();
					while (!Check(")")) {
						args.Add(ParseExpression());
						SkipNewlines();

						if (Check(",")) {
							Advance();
							SkipNewlines();
						} else break;
					}

					Expect(")");
					Advance();
					
					return new FunctionCall(start_token, varName, args.ToArray());
				}

				return new VariableNode(start_token, varName);
			}

			// parentheses
			if (token == "(") {
				Advance(); // Consume `(`
				SkipNewlines();

				Node expr = ParseExpression();
				SkipNewlines();

				if (PeekAhead()?.Value == ",") {
					Advance();
					SkipNewlines();

					var expressions = new List<Node> { expr };

					while (!Check(")")) {
						expressions.Add(ParseExpression());
						SkipNewlines();

						if (PeekAhead()?.Value == ",") {
							Advance();
							SkipNewlines();
						} else break;
					}

					expr = new ListNode(start_token, expressions, true);
				}

				Expect(")");
				Advance();
				return expr;
			}

			throw new ParseError($"Unexpected token '{(token == "\n" ? "\\n" : token)}'", start_token);
		}

		private FunctionNode ParseLambda() {
			Token start_token = PeekAhead()!;
			Expect("(");
			Advance();

			var args = new List<string>();
			while (!Check(")")) {
				SkipNewlines();
				var argTok = PeekAhead();
				if (!IsIdentifier(argTok)) {
					throw new ParseError($"Expected parameter name, got '{TokenToDisplay(argTok)}'", argTok!);
				}

				args.Add(Advance()!.Value);
				SkipNewlines();

				if (Check(",")) {
					Advance();
					SkipNewlines();
				} else break;
			}

			Expect(")");
			Advance();

			return new FunctionNode(start_token, args.ToArray(), ParseBlock());
		}

		private bool IsNewline() => PeekAhead()?.Value == "\n";

		private void SkipNewlines() {
			while (PeekAhead() != null && IsNewline()) Advance();
		}

		private static bool IsIdentifier(Token? token) {
			if (token == null) return false;
			var v = token.Value;
			if (v.Length == 0) return false;
			return (char.IsLetter(v[0]) || v[0] == '_') && v.All(c => char.IsLetterOrDigit(c) || c == '_');
		}

		private static bool IsString(string? token) {
			if (token == null) return false;
			var Quotes = Lexer.QUOTES;
			return token.Length >= 2 && Quotes.Contains(token[0]) && Quotes.Contains(token[^1]);
		}
	}
}