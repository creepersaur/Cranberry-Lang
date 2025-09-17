using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;
using Cranberry.Types;

namespace Cranberry.Namespaces;

public class N_Task : CNamespace {
	private Interpreter Interpreter;
	
	public N_Task(Interpreter interpreter) : base("Task", true) {
		Interpreter = interpreter;
		
		Members = new Dictionary<string, object?> {
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
					if (args is not [FunctionNode func])
						throw new RuntimeError("Spawn(fn) expects 1 function argument.");

					var thread = new Thread(() => Interpreter.VisitFunctionCall(new FunctionCall("", []) {
						Target = func
					}));
					thread.Start();
					return new NullNode();
				})
			}
		};
	}
}