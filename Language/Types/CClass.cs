using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CClass(string name, FunctionNode? constructor, Interpreter interpreter) : IMemberAccessible {
	public readonly string Name = name;
	public readonly FunctionNode? Constructor = constructor;
	public readonly Dictionary<string, FunctionNode> Functions = new();

	public InternalFunction GetCreateFunction() {
		return new InternalFunction(callArgs => {
			var obj = new CObject(this);

			if (Constructor != null) {
				var callList = new List<object?>();
				callList.Add(obj);
				callList.AddRange(callArgs);
			
				// Execute constructor similarly to Interpreter.VisitFunctionCall:
				interpreter.env.Push();
				try {
					if (Constructor.Env != null) interpreter.env.Push(Constructor.Env);

					// Bind constructor args (missing args => NullNode)
					for (int i = 0; i < Constructor.Args.Length; i++) {
						object? value = i < callList.Count ? callList[i] : new NullNode();
						interpreter.env.Define(Constructor.Args[i], value);
					}

					try {
						interpreter.Evaluate(Constructor.Block);
					} catch (ReturnException) {
					} catch (OutException) {
					} finally {
						if (Constructor.Env != null) interpreter.env.Pop();
					}
				} finally {
					interpreter.env.Pop();
				}
			}

			return obj;
		});
	}
	
	public object GetMember(object member) {
		if (member is string m) {
			if (Functions.TryGetValue(m, out var node)) {
				return node;
			}
		}
		
		throw new RuntimeError($"Tried to get unknown member: `{member}` on `Class:{Name}`.");
	}

	public override string ToString() => $"Class:{Name}";
}