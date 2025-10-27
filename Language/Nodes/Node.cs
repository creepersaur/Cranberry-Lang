using Cranberry.Builtin;
using Cranberry.Types;

namespace Cranberry.Nodes;

public abstract class Node(Token? start_token) : IMemberAccessible {
	public readonly Token StartToken = start_token ?? new Token("<unknown>", -1, 1, "<unknown>", "<unknown>");
	public abstract object? Accept<T>(INodeVisitor<T> visitor);
}

public interface INodeVisitor<out T> {
	// Types
	
	T? VisitNull(NullNode node);
	T VisitNumber(NumberNode node);
	T VisitString(StringNode node);
	T VisitBool(BoolNode node);
	T VisitFunction(FunctionNode node);
	CRange VisitRange(RangeNode node);
	CList VisitList(ListNode node);
	CDict VisitDict(DictNode node);
	
	// Operations
	
	T VisitBinaryOp(BinaryOpNode node);
	T VisitUnaryOp(UnaryOpNode node);
	T VisitVariable(VariableNode node);
	T VisitAssignment(AssignmentNode node);
	T VisitShorthandAssignment(ShorthandAssignmentNode node);
	T? VisitFunctionCall(FunctionCall node);
	T? VisitBlock(BlockNode node);
	T? VisitMemberAccess(MemberAccessNode node);
	T? VisitMemberAssignment(MemberAssignmentNode node);
	T? VisitMemberShorthandAssignment(MemberShorthandAssignmentNode node);
	T? VisitInternalFunction(InternalFunction node);
	T VisitFallback(FallbackNode node);
	T VisitTernaryCondition(TernaryConditionNode node);
	T VisitCast(CastNode node);
	void VisitSignal(SignalNode node);
	
	// Statements
	
	T? VisitLet(LetNode node, bool no_define);
	T? VisitIF(IFNode node);
	T? VisitFunctionDef(FunctionDef node);
	T? VisitClassDef(ClassDef node);
	T? VisitReturn(ReturnNode node);
	T? VisitBreak(BreakNode node);
	T? VisitContinue(ContinueNode node);
	T? VisitOut(OutNode node);
	T? VisitScope(ScopeNode node);
	T? VisitWhile(WhileNode node);
	T? VisitFOR(ForNode node);
	T? VisitSwitch(SwitchNode node);
	T? VisitUsingDirective(UsingDirective node);
	T? VisitNamespaceDirective(NamespaceDirective node);
	T? VisitDecorator(DecoratorNode node);
}