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
	T VisitFunction(FunctionNode node);
	
	// Operations
	
	T VisitBinaryOp(BinaryOpNode node);
	T VisitUnaryOp(UnaryOpNode node);
	T VisitVariable(VariableNode node);
	T VisitAssignment(AssignmentNode node);
	T VisitShorthandAssignment(ShorthandAssignmentNode node);
	T? VisitFunctionCall(FunctionCall node);
	
	// Statements
	
	T? VisitLet(LetNode node);
	T? VisitIF(IFNode node);
	T? VisitFunctionDef(FunctionDef node);
	T? VisitReturn(ReturnNode node);
	T? VisitScope(ScopeNode node);
}