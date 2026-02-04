using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Types;

namespace Cranberry.Namespaces;

public class N_Math : CNamespace {
	public N_Math() : base("Math", true) {
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

		// registration helpers
		void AddConst(string name, object value) => table[name] = value;

		void AddUnary(string name, Func<double, object> impl) {
			table[name] = new InternalFunction((_, args) => {
				ExpectCount(name, args, 1);
				ExpectNumber(name, args[0], 0);
				return impl(ToDouble(args[0]));
			});
		}

		void AddBinary(string name, Func<double, double, object> impl) {
			table[name] = new InternalFunction((_, args) => {
				ExpectCount(name, args, 2);
				ExpectNumber(name, args[0], 0);
				ExpectNumber(name, args[1], 1);
				return impl(ToDouble(args[0]), ToDouble(args[1]));
			});
		}

		void AddTernary(string name, Func<double, double, double, object> impl) {
			table[name] = new InternalFunction((_, args) => {
				ExpectCount(name, args, 3);
				ExpectNumber(name, args[0], 0);
				ExpectNumber(name, args[1], 1);
				ExpectNumber(name, args[2], 2);
				return impl(ToDouble(args[0]), ToDouble(args[1]), ToDouble(args[2]));
			});
		}

		// varargs, returns double (or whatever)
		void AddVarArgs(string name, Func<double[], object> impl) {
			table[name] = new InternalFunction((_, args) => {
				ExpectMinCount(name, args, 1);
				var arr = new double[args.Length];
				for (int i = 0; i < args.Length; i++) {
					ExpectNumber(name, args[i], i);
					arr[i] = ToDouble(args[i]);
				}

				return impl(arr);
			});
		}

		// Add commonly used constants
		AddConst("pi", Math.PI);
		AddConst("e", Math.E);
		AddConst("tau", Math.PI * 2.0);

		// Simple unary/binary functions
		AddBinary("max", (a, b) => Math.Max(a, b));
		AddBinary("min", (a, b) => Math.Min(a, b));
		AddTernary("clamp", (v, lo, hi) => Math.Clamp(v, lo, hi));

		AddUnary("rad", x => double.DegreesToRadians(x));
		AddUnary("deg", x => double.RadiansToDegrees(x));
		AddUnary("sin", x => Math.Sin(x));
		AddUnary("cos", x => Math.Cos(x));
		AddUnary("tan", x => Math.Tan(x));
		AddUnary("asin", x => Math.Asin(x));
		AddUnary("acos", x => Math.Acos(x));
		AddUnary("atan", x => Math.Atan(x));
		AddUnary("asinh", x => Math.Asinh(x));
		AddUnary("acosh", x => Math.Acosh(x));
		AddUnary("atanh", x => Math.Atanh(x));
		AddBinary("atan2", (y, x) => Math.Atan2(y, x));
		
		AddUnary("abs", x => Math.Abs(x));
		AddUnary("floor", x => Math.Floor(x));
		AddUnary("ceil", x => Math.Ceiling(x));
		AddUnary("round", x => Math.Round(x));
		AddUnary("exp", x => Math.Exp(x));
		AddBinary("pow", (a, b) => Math.Pow(a, b));
		AddUnary("sqrt", x => Math.Sqrt(x));
		
		// Wrapped Add/Sub
		AddTernary("wrapped_add", (value, addAmount, wrapAt) => {
			var result = (value + addAmount) % wrapAt;
        
			// Handle negative results
			if (result < 0)
				result += wrapAt;
        
			return result;
		});
		AddTernary("wrapped_sub", (value, addAmount, wrapAt) => {
			var result = (value - addAmount) % wrapAt;
        
			// Handle negative results
			if (result < 0)
				result += wrapAt;
        
			return result;
		});

		// Lerp: a + (b - a) * t
		table["lerp"] = new InternalFunction((_, args) => {
			ExpectCount("lerp", args, 3);
			ExpectNumber("lerp", args[0], 0);
			ExpectNumber("lerp", args[1], 1);
			ExpectNumber("lerp", args[2], 2);
			var a = ToDouble(args[0]);
			var b = ToDouble(args[1]);
			var t = ToDouble(args[2]);
			return a + (b - a) * t;
		});

		// Example varargs overloads
		AddVarArgs("max_all", arr => {
			double m = arr[0];
			for (int i = 1; i < arr.Length; i++)
				if (arr[i] > m)
					m = arr[i];
			return m;
		});
		AddVarArgs("min_all", arr => {
			double m = arr[0];
			for (int i = 1; i < arr.Length; i++)
				if (arr[i] < m)
					m = arr[i];
			return m;
		});

		// push dictionary into the env
		env.Variables.Push(table);
	}
}