using Cranberry.Errors;

namespace Cranberry.Types;

public class CRange(double start, double end, double step, bool inclusive) : CustomType, IMemberAccessible {
	public readonly double Start = start;
	public readonly double End = end;
	public readonly double Step = step;
	public readonly bool Inclusive = inclusive;

	public override string ToString() => $"Range<{Start} .. {End}, {Step}>";
	
	/////////////////////////////////////////////////////////
	// MEMBERSHIP
	///////////////////////////////////////////////////////// 

	public object GetMember(string name) {
		if (name == "length") return Math.Abs(Start - End);
		
		throw new RuntimeError($"Tried getting unknown member `{name}` on type `Range`");
	}
}