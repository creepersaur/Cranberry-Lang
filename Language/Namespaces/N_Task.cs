using System.Diagnostics;
using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;
using Cranberry.Types;

namespace Cranberry.Namespaces;

public class N_Task : CNamespace {
	public N_Task(Interpreter interpreter) : base("Task", true) {
		var interpreter1 = interpreter;

		env.Variables.Push(new Dictionary<string, object> {
			["wait"] = new InternalFunction((_, args) => {
				if (args.Length != 1 || !Misc.IsNumber(args[0]!))
					throw new RuntimeError("wait(seconds) expects 1 number argument.");

				double seconds = Convert.ToDouble(args[0]!);
				if (seconds <= 0) return new NullNode();

				// total requested milliseconds (keep as double)
				double totalMs = seconds * 1000.0;

				// Reserve a small amount for spinning (final fine-grained wait)
				const int reserveMsForSpin = 3; // tune between 1..6 depending on your needs
				int coarseMs = (int)Math.Floor(Math.Max(0.0, totalMs - reserveMsForSpin));

				if (coarseMs > 0)
					Thread.Sleep(coarseMs);

				double remainingMs = totalMs - coarseMs;
				if (remainingMs > 0) {
					var sw = Stopwatch.StartNew();
					// Spin until the remaining time has elapsed.
					// SpinWait iterations can be adjusted — using a tiny spin helps reduce busy-loop CPU
					while (sw.Elapsed.TotalMilliseconds < remainingMs) {
						Thread.SpinWait(10);
					}
				}

				return new NullNode();
			}),
			["wait_milliseconds"] = new InternalFunction((_, args) => {
				if (args.Length != 1 || !Misc.IsNumber(args[0]!))
					throw new RuntimeError("wait_milliseconds(ms) expects 1 number argument.");

				double ms = Convert.ToDouble(args[0]!);
				if (ms <= 0) return new NullNode();

				const int reserveMsForSpin = 2;
				int coarseMs = (int)Math.Floor(Math.Max(0.0, ms - reserveMsForSpin));

				if (coarseMs > 0)
					Thread.Sleep(coarseMs);

				double remainingMs = ms - coarseMs;
				if (remainingMs > 0) {
					var sw = Stopwatch.StartNew();
					while (sw.Elapsed.TotalMilliseconds < remainingMs) {
						Thread.SpinWait(10);
					}
				}

				return new NullNode();
			}),
			["now"] = new InternalFunction((_, args) => {
				if (args.Length != 0)
					throw new RuntimeError("now() expects 0 arguments.");

				return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
			}),

			["spawn"] = new InternalFunction((_, args) => {
				if (args.Length < 1 || args[0] is not FunctionNode func)
					throw new RuntimeError("spawn(fn, ...) expects first argument as function.");

				var new_args = args.ToList();
				new_args.RemoveAt(0);

				var task = Task.Run(() => interpreter1.VisitFunctionCall(new FunctionCall(null, "", [.. new_args]) {
					Target = func
				}));

				return new CTask(task);
			}),

			["Thread"] = new InternalFunction((_, args) => {
				if (args.Length < 1 || args[0] is not FunctionNode func)
					throw new RuntimeError("Thread(fn, ...) expects first argument as function.");

				var new_args = args.ToList();
				new_args.RemoveAt(0);

				CThread cthread = null!;

				var thread = new Thread(() => {
					try {
						var result = interpreter1.VisitFunctionCall(new FunctionCall(null, "", [.. new_args]) {
							Target = func
						});

						cthread.SetResult(result);
					} catch (Exception ex) {
						cthread.SetException(ex);
					}
				});

				cthread = new CThread(thread);

				thread.Start();
				return cthread;
			}),

			["stopwatch"] = new InternalFunction((_, args) => {
				if (args.Length != 0)
					throw new RuntimeError("stopwatch() expects 0 arguments.");

				return new CStopwatch(new Stopwatch());
			})
		});
	}
}