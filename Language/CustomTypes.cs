namespace Cranberry.Types;

public abstract class CustomType;

public class Range(double start, double end, double step) : CustomType {
	public readonly double Start = start;
	public readonly double End = end;
	public readonly double Step = step;

	public override string ToString() => $"Range<{Start} .. {End}, {Step}>";
}