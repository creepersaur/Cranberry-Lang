using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CRange(double start, double end, double step, bool inclusive) : IMemberAccessible {
	public readonly double Start = start;
	public readonly double End = end;
	public readonly double Step = step;
	public readonly bool Inclusive = inclusive;

	public override string ToString() => $"Range<{Start} .. {End}, {Step}>";
	
	/////////////////////////////////////////////////////////
	// MEMBERSHIP
	///////////////////////////////////////////////////////// 

	public object GetMember(object member) {
		string name = Convert.ToString(member)!;
		return name switch {
			"length" => new NumberNode(Math.Abs(Start - End)),
			"start" => new NumberNode(Start),
			"end" => new NumberNode(End),
			"step" => new NumberNode(Step),
			"inclusive" => new BoolNode(Inclusive),
			
			_ => throw new RuntimeError($"Tried getting unknown member `{member}` on type `Range`")
		};
	}
}