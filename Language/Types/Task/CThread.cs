using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CThread : IMemberAccessible {
	private Thread _thread;
	private Exception? _exception;
	private object? _result;
	private bool _started = false;

	public override string ToString() => $"CThread<alive: {_thread.IsAlive}>";

	public CThread(Thread thread) {
		_thread = thread ?? throw new RuntimeError($"Failed to create thread: {nameof(thread)}");
		_started = thread.ThreadState != ThreadState.Unstarted;
	}

	public void SetResult(object? res) => _result = res;
	public void SetException(Exception ex) => _exception = ex;

	/////////////////////////////////////////////////////////
	// MEMBERSHIP
	///////////////////////////////////////////////////////// 

	public object GetMember(object? member) {
		if (member is string name)
			return name switch {
				"is_background" => new InternalFunction((_, _) => _thread.IsBackground),
				"is_alive" => new InternalFunction((_, _) => _started && !_thread.IsAlive),
				"is_completed" => new InternalFunction((_, _) => !_thread.IsAlive),
				"is_faulted" => new InternalFunction((_, _) => _exception != null),

				// wait() blocks until the thread finishes
				"wait" => new InternalFunction((_, args) => {
					if (args.Length != 0)
						throw new RuntimeError("`wait()` expects 0 arguments.");

					_thread.Join();

					return new NullNode();
				}),

				// wait_timeout(seconds) -> returns true if finished, false if timed out
				"wait_timeout" => new InternalFunction((_, args) => {
					if (args.Length != 1 || !Misc.IsNumber(args[0]!))
						throw new RuntimeError("`wait_timeout(seconds)` expects 1 number argument.");

					double seconds = Convert.ToDouble(args[0]!);
					if (seconds < 0) throw new RuntimeError("`seconds` argument must be >= 0");

					int ms = (int)Math.Ceiling(seconds * 1000.0);
					bool finished = _thread.Join(ms);

					return finished;
				}),

				// error() -> throw or return error message
				"error" => new InternalFunction((_, args) => {
					if (args.Length != 0) throw new RuntimeError("`error()` expects 0 arguments.");
					if (_exception == null) return new NullNode();
					return _exception.Message;
				}),

				// result() -> block until done and return result if any (or NullNode)
				"result" => new InternalFunction((_, args) => {
					if (args.Length != 0) throw new RuntimeError("`result()` expects 0 arguments.");
					_thread.Join();
					return _result ?? new NullNode();
				}),

				_ => throw new RuntimeError($"Tried getting unknown member `{member}` on type `CThread`")
			};

		throw new RuntimeError($"Tried getting unknown member `{member}` on type `CThread`");
	}
}