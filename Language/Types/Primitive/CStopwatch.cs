using System.Diagnostics;
using Cranberry.Builtin;
using Cranberry.Errors;

namespace Cranberry.Types;

public class CStopwatch(Stopwatch Stopwatch) : IMemberAccessible {
	public object? GetMember(object? member) {
		if (member is string m) {
			switch (m) {
				case "microseconds": return Stopwatch.Elapsed.TotalMicroseconds;
				case "nanoseconds": return Stopwatch.Elapsed.TotalNanoseconds;
				case "milliseconds": return Stopwatch.Elapsed.TotalMilliseconds;
				case "seconds": return Stopwatch.Elapsed.TotalSeconds;
				case "elapsed": return Stopwatch.Elapsed.TotalSeconds;
				case "minutes": return Stopwatch.Elapsed.TotalMinutes;
				case "hours": return Stopwatch.Elapsed.TotalHours;
				case "days": return Stopwatch.Elapsed.TotalDays;
				
				case "start":
					return new InternalFunction(args => {
						if (args.Length > 0)
							throw new RuntimeError("`start() expects 0 arguments.");
						
						Stopwatch.Start();
						return this;
					});
				
				case "stop":
					return new InternalFunction(args => {
						if (args.Length > 0)
							throw new RuntimeError("`stop() expects 0 arguments.");

						Stopwatch.Stop();
						return this;
					});
				
				case "restart":
					return new InternalFunction(args => {
						if (args.Length > 0)
							throw new RuntimeError("`restart() expects 0 arguments.");

						Stopwatch.Restart();
						return this;
					});
				
				case "reset":
					return new InternalFunction(args => {
						if (args.Length > 0)
							throw new RuntimeError("`reset() expects 0 arguments.");

						Stopwatch.Reset();
						return this;
					});
				
				case "start_new":
					return new InternalFunction(args => {
						if (args.Length > 0)
							throw new RuntimeError("`start_new() expects 0 arguments.");
						
						return new CStopwatch(Stopwatch.StartNew());
					});
			}
		}

		throw new RuntimeError($"Cannot get member `{member}` on Stopwatch.");
	}
}