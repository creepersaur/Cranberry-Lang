using Cranberry.Nodes;
// ReSharper disable LoopCanBeConvertedToQuery
namespace Cranberry.Builtin;

public static class BuiltinFunctions {
	public static Node Print(object?[] args, bool new_line = false) {
		string output = "";
		
		foreach (var t in args) {
			output += Convert.ToString(t) + " ";
		}

		if (new_line) {
			Console.WriteLine(output[..^1]);
		} else {
			Console.Write(output[..^1]);
		}

		return new NullNode();
	}
}