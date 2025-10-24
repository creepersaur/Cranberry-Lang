using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CSignal(Interpreter interpreter) : IMemberAccessible {
	public readonly List<object> Connections = [];

	public override string ToString() => "Signal";

	public object GetMember(object? member) {
		if (member is string m) {
			switch (m) {
				case "connections": return Connections;
				
				case "emit":
					return new InternalFunction((_, args) => {
						foreach (var connection in Connections) {
							if (connection is FunctionNode f)
								interpreter.Evaluate(new FunctionCall(null, "", args) {
									Target = f
								});
							if (connection is InternalFunction i)
								interpreter.Evaluate(new FunctionCall(null, "", args) {
									Target = i
								});
						}

						return new NullNode();
					});
				
				case "connect":
					return new InternalFunction((_, args) => {
						if (args.Length != 1)
							throw new RuntimeError("`signal.connect(fn)` expects 1 argument.");

						if (args[0] is FunctionNode f) {
							Connections.Add(f);
							return new NullNode();
						}
						if (args[0] is InternalFunction i) {
							Connections.Add(i);
							return new NullNode();
						}
							
						throw new RuntimeError("`signal.connect(fn)` expects 1 function argument.");
					});
				
				case "disconnect":
					return new InternalFunction((_, args) => {
						if (args.Length != 1)
							throw new RuntimeError("`signal.disconnect(fn)` expects 1 argument.");

						if (Connections.Contains(args[0]!)) {
							Connections.Remove(args[0]!);
							return new NullNode();
						}
							
						throw new RuntimeError("`signal.disconnect(fn)` expects 1 function argument.");
					});
				
				case "disconnect_all":
					return new InternalFunction((_, _) => {
						Connections.Clear();
						return new NullNode();
					});
			}
		}

		throw new RuntimeError($"Cannot get member `{member}` on Stopwatch.");
	}
}