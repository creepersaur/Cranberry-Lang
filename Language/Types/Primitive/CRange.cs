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

	public object GetMember(object? member) {
		return member switch {
			"length" => new NumberNode(null, Math.Abs(Start - End)),
			"start" => new NumberNode(null, Start),
			"end" => new NumberNode(null, End),
			"step" => new NumberNode(null, Step),
			"inclusive" => new BoolNode(null, Inclusive),
			
			_ => throw new RuntimeError($"Tried getting unknown member `{member}` on type `Range`")
		};
	}
}