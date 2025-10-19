using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Types;

namespace Cranberry.Namespaces;

public class N_Random : CNamespace {
	private const double TOLERANCE = 1e-9;

	public N_Random() : base("Random", true) {
		var table = new Dictionary<string, object>();

		// helper conversions / validators
		static double ToDouble(object? o) => Convert.ToDouble(o);

		static void ExpectCount(string name, object?[] args, int expected) {
			if (args.Length != expected)
				throw new RuntimeError($"{name}() expects {expected} argument{(expected == 1 ? "" : "s")}, got {args.Length}");
		}

		static void ExpectMinCount(string name, object?[] args, int min) {
			if (args.Length < min)
				throw new RuntimeError($"{name}() expects at least {min} argument{(min == 1 ? "" : "s")}, got {args.Length}");
		}

		static void ExpectNumber(string name, object? obj, int index) {
			if (!Misc.IsNumber(obj!))
				throw new RuntimeError($"{name}() expects argument #{index + 1} to be a number.");
		}

		void AddNoArgs(string name, Func<object> impl) {
			table[name] = new InternalFunction(args => {
				ExpectCount(name, args, 0);
				return impl();
			});
		}

		void AddBinary(string name, Func<double, double, object> impl) {
			table[name] = new InternalFunction(args => {
				ExpectCount(name, args, 2);
				ExpectNumber(name, args[0], 0);
				ExpectNumber(name, args[1], 1);
				return impl(ToDouble(args[0]), ToDouble(args[1]));
			});
		}

		// varargs, returns double (or whatever)
		void AddVarArgs(string name, Func<double[], object> impl) {
			table[name] = new InternalFunction(args => {
				ExpectMinCount(name, args, 1);
				var arr = new double[args.Length];
				for (int i = 0; i < args.Length; i++) {
					ExpectNumber(name, args[i], i);
					arr[i] = ToDouble(args[i]);
				}

				return impl(arr);
			});
		}

		// Simple unary/binary functions
		AddNoArgs("float", () => {
			int max = 1000000; // number of steps
			return Random.Shared.Next(0, max + 1) / max;
		});
		AddBinary("float_range", (min, max) => {
			int max_tolerance = 1000000; // number of steps
			double value = Random.Shared.Next(0, max_tolerance + 1) / (double)max_tolerance; // [0.0, 1.0] inclusive
			return Math.Min(min, max) + value * Math.Abs(max - min);
		});

		AddNoArgs("int", () => (double)Random.Shared.NextInt64());
		AddBinary("int_range",
			(min, max) => (double)Random.Shared.NextInt64(
				Convert.ToInt64(Math.Abs(min - max) < TOLERANCE ? min : Math.Max(min, max)),
				Convert.ToInt64(Math.Abs(min - max) < TOLERANCE ? max : Math.Max(min, max)) + 1
			)
		);

		AddVarArgs("random_argument", args => args[Random.Shared.NextInt64(0, args.Length)]);

		// push dictionary into the env
		env.Variables.Push(table);
	}
}