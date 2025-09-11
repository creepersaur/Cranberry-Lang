namespace Cranberry.Nodes;

public abstract class Node {
	public abstract T Accept<T>(INodeVisitor<T> visitor);
}

public interface INodeVisitor<out T> {
	// Types
	
	T? VisitNull(NullNode node);
	T VisitNumber(NumberNode node);
	T VisitString(StringNode node);
	T VisitBool(BoolNode node);
	
	// Operations
	
	T VisitVariable(VariableNode node);
	T VisitBinaryOp(BinaryOpNode node);
	T VisitUnaryOp(UnaryOpNode node);
	
	// Statements
	
	T? VisitLet(LetNode node);
	T VisitAssignment(AssignmentNode node);
	T VisitShorthandAssignment(ShorthandAssignmentNode node);
	T? VisitIF(IFNode node);
}