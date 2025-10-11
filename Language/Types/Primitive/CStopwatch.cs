using System.Diagnostics;
using Cranberry.Builtin;
using Cranberry.Errors;

namespace Cranberry.Types;

public class CStopwatch(Stopwatch Stopwatch) : IMemberAccessible {
	public object? GetMember(object? member) {
		if (member is string m) {
			switch (m) {
				case "Microseconds": return Stopwatch.Elapsed.TotalMicroseconds;
				case "Nanoseconds": return Stopwatch.Elapsed.TotalNanoseconds;
				case "Milliseconds": return Stopwatch.Elapsed.TotalMilliseconds;
				case "Seconds": return Stopwatch.Elapsed.TotalSeconds;
				case "Elapsed": return Stopwatch.Elapsed.TotalSeconds;
				case "Minutes": return Stopwatch.Elapsed.TotalMinutes;
				case "Hours": return Stopwatch.Elapsed.TotalHours;
				case "Days": return Stopwatch.Elapsed.TotalDays;
				
				case "Start":
					return new InternalFunction(args => {
						if (args.Length > 0)
							throw new RuntimeError("`Start() expects 0 arguments.");
						
						Stopwatch.Start();
						return this;
					});
				
				case "Stop":
					return new InternalFunction(args => {
						if (args.Length > 0)
							throw new RuntimeError("`Stop() expects 0 arguments.");

						Stopwatch.Stop();
						return this;
					});
				
				case "Restart":
					return new InternalFunction(args => {
						if (args.Length > 0)
							throw new RuntimeError("`Restart() expects 0 arguments.");

						Stopwatch.Restart();
						return this;
					});
				
				case "Reset":
					return new InternalFunction(args => {
						if (args.Length > 0)
							throw new RuntimeError("`Restart() expects 0 arguments.");

						Stopwatch.Reset();
						return this;
					});
				
				case "StartNew":
					return new InternalFunction(args => {
						if (args.Length > 0)
							throw new RuntimeError("`Restart() expects 0 arguments.");
						
						return new CStopwatch(Stopwatch.StartNew());
					});
			}
		}

		throw new RuntimeError($"Cannot get member `{member}` on Stopwatch.");
	}
}