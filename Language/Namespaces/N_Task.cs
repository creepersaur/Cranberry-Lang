using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;
using Cranberry.Types;

namespace Cranberry.Namespaces;

public class N_Task : CNamespace {
	public N_Task(Interpreter interpreter) : base("Task", true) {
		var interpreter1 = interpreter;
		
		env.Variables.Push(new Dictionary<string, object> {
			//////////////////////////////////////////////////////////
			// METHODS
			//////////////////////////////////////////////////////////

			{
				"Wait", new InternalFunction(args => {
					if (args.Length != 1 || !Misc.IsNumber(args[0]!))
						throw new RuntimeError("Wait(seconds) expects 1 number argument.");

					Thread.Sleep(Convert.ToInt32(Convert.ToDouble(args[0]!) * 1000));
					return new NullNode();
				})
			}, {
				"WaitMilliseconds", new InternalFunction(args => {
					if (args.Length != 1 || !Misc.IsNumber(args[0]!))
						throw new RuntimeError("Wait(seconds) expects 1 number argument.");

					Thread.Sleep(Convert.ToInt32(args[0]!));
					return new NullNode();
				})
			}, {
				"Now", new InternalFunction(args => {
					if (args.Length != 0)
						throw new RuntimeError("Now() expects 0 arguments.");

					return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				})
			}, {
				"Spawn", new InternalFunction(args => {
					if (args.Length < 1 || args[0] is not FunctionNode func)
						throw new RuntimeError("Spawn(fn, ...) expects first argument as function.");
					
					var new_args = args.ToList();
					new_args.RemoveAt(0);

					var thread = new Thread(() => interpreter1.VisitFunctionCall(new FunctionCall("", new_args.ToArray()) {
						Target = func
					}));
					thread.Start();
					return new NullNode();
				})
			}
		});
	}
}