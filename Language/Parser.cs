using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry;

public class Parser(string[] Tokens) {
    private int Pos = -1;

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
    private bool Check(string token, int offset = 1) {
        return PeekAhead(offset) == token;
    }

    // Advances and returns the token
    private string? Advance() {
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

        return new IFNode(condition, then.ToArray());
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

    
    ////////////////////////////////////////////////////////////
    // HANDLE EXPRESSIONS
    ////////////////////////////////////////////////////////////
    public Node ParseExpression() {
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

        throw new ParseError($"Unexpected token '{token}'", Pos);
    }
    
    private static bool IsIdentifier(string? token) {
        return token != null && 
               char.IsLetter(token[0]) && 
               token.All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}