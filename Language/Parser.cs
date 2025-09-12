using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry;

public class Parser(string[] Tokens) {
    private int Pos = -1;
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
        
        if (token == "if") {
            return ParseIF();
        }

        if (token == "fn") {
            return ParseFunctionDef();
        }

        if (token == "return") {
            Advance();
            return new ReturnNode(ParseExpression());
        }

        if (token == "{") {
            return ParseScope();
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

    private Node ParseScope() {
        Advance();
        
        var statements = new List<Node>();
        while (!Check("}")) {
            statements.Add(Parse());
            if (Check(";"))
                Advance();
        }
        
        Expect("}");
        Advance();

        return new ScopeNode(statements.ToArray());
    }

    private Node ParseFunctionDef() {
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

        return new FunctionDef(func_name!, args.ToArray(), statements.ToArray());
    }

    private Node ParseIF() {
        Advance();
        var condition = ParseExpression();
        
        Expect("{");
        Advance();

        var then = new List<Node>();
        while (!Check("}")) {
            then.Add(Parse());
        }

        Advance();

        var elif = new List<(Node, Node[])>();
        var else_statements = new List<Node>();
        
        while (Check("else")) {
            Advance();
            if (Check("if")) {
                Advance();
                var elif_condition = ParseExpression();
                
                Expect("{");
                Advance();

                var elif_statements = new List<Node>();
                while (!Check("}")) {
                    elif_statements.Add(Parse());
                }

                Advance();
                
                elif.Add((elif_condition, elif_statements.ToArray()));
            } else {
                Expect("{");
                Advance();
                
                while (!Check("}")) {
                    else_statements.Add(Parse());
                }

                Advance();

                break;
            }
        }

        return new IFNode(condition, then.ToArray(), elif.ToArray(), else_statements.ToArray());
    }

    private Node ParseLet() {
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
    
    private Node ParseAssignment() {
        string varName = Advance()!;
        Advance(); // consume `=`
        
        Node value = ParseExpression(); // right side
        return new AssignmentNode(varName, value);
    }
    
    private Node ParseShorthandAssignment() {
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
        Node left = ParseUnary();

        while (Pos < Tokens.Length) {
            string? op = PeekAhead();
            if (op != "*" && op != "/" && op != "%" && op != "//") break;

            Advance();
            Node right = ParseUnary();
            left = new BinaryOpNode(left, op, right);
        }

        return left;
    }

    private Node ParseUnary() {
        string? token = PeekAhead();

        if (token is "-" or "+") {
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
        if (Check("(")) {
            Advance(); // consume '('
        
            var args = new List<Node>();
            while (!Check(")")) {
                args.Add(ParseExpression());
                
                if (Check(",")) {
                    Advance();
                } else {
                    break;
                }
            }
        
            Expect(")");
            Advance();
        
            return new FunctionCall("", args.ToArray()) { 
                Target = node // Store what we're calling
            };
        }
    
        return node;
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

        if (token == "fn" && Check("(", 2)) {
            return ParseLambda();
        }

        // variable (identifier)
        if (IsIdentifier(token)) {
            Advance();
            
            if (Check("(")) {
                Advance(); // Consume `(`

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
    
    private Node ParseLambda() {
        Advance(); // consume 'fn'
    
        Expect("(");
        Advance();
    
        var args = new List<string>();
        if (!Check(")")) {
            do {
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
            } while (!Check(")"));
        }
    
        Expect(")");
        Advance();
    
        Expect("{");
        Advance();
    
        var statements = new List<Node>();
        while (!Check("}")) {
            statements.Add(Parse());
            if (Check(";"))
                Advance();
        }
    
        Expect("}");
        Advance(); // consume '}'
    
        return new FunctionNode(args.ToArray(), statements.ToArray());
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