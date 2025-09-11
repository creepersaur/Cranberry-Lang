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
        
        if (IsIdentifier(token) && Check("=", 2)) {
            return ParseAssignment();
        }
        
        if (IsIdentifier(token) && SHORTHANDS.Contains(PeekAhead(2))) {
            return ParseShorthandAssignment();
        }

        return ParseExpression();
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
        var name = Advance();
        if (!IsIdentifier(name)) {
            throw new ParseError($"Expected identifier as variable name, got: `{name}`", Pos + 1);
        }
        
        // Initialization
        if (Check("=")) {
            Advance();
            var value = ParseExpression();

            return new LetNode(name!, value);
        }

        return new LetNode(name!, new NullNode());
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
            if (op != "*" && op != "/" && op != "%") break;

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
        }

        if (IsString(token)) {
            Advance();
            return new StringNode(token![1..^1]);
        }

        // variable (identifier)
        if (IsIdentifier(token)) {
            Advance();
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