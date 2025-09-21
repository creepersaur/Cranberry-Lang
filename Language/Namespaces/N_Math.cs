using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Types;

namespace Cranberry.Namespaces;

public class N_Math : CNamespace {
	public N_Math() : base("Math", true) {
		env.Variables.Push(new Dictionary<string, object> {
			["PI"] = Math.PI,
			["E"] = Math.E,
			["Tau"] = Math.Tau,

			//////////////////////////////////////////////////////////
			// METHODS
			//////////////////////////////////////////////////////////

			["Max"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Max() expects 2 arguments");
				if (!Misc.IsNumber(args[0]!) || !Misc.IsNumber(args[1]!))
					throw new RuntimeError("Max() expects a number.");

				return Math.Max(Convert.ToDouble(args[0]), Convert.ToDouble(args[1]));
			}),
			["Min"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Min() expects 2 arguments");
				if (!Misc.IsNumber(args[0]!) || !Misc.IsNumber(args[1]!))
					throw new RuntimeError("Min() expects a number.");

				return Math.Min(Convert.ToDouble(args[0]), Convert.ToDouble(args[1]));
			}),
			["Clamp"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Clamp() expects 3 arguments");
				if (!Misc.IsNumber(args[0]!) || !Misc.IsNumber(args[1]!) || !Misc.IsNumber(args[2]!))
					throw new RuntimeError("Clamp() expects 3 numbers.");

				return Math.Clamp(Convert.ToDouble(args[0]), Convert.ToDouble(args[1]), Convert.ToDouble(args[2]));
			}),
			["Sin"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Sin() expects 1 argument");
				if (!Misc.IsNumber(args[0]!))
					throw new RuntimeError("Sin() expects a number.");

				return Math.Sin(Convert.ToDouble(args[0]));
			}),
			["Cos"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Cos() expects 1 argument");
				if (!Misc.IsNumber(args[0]!))
					throw new RuntimeError("Cos() expects a number.");

				return Math.Cos(Convert.ToDouble(args[0]));
			}),
			["Tan"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Tan() expects 1 argument");
				if (!Misc.IsNumber(args[0]!))
					throw new RuntimeError("Tan() expects a number.");

				return Math.Tan(Convert.ToDouble(args[0]));
			}),
			["Asin"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Asin() expects 1 argument");
				if (!Misc.IsNumber(args[0]!))
					throw new RuntimeError("Asin() expects a number.");

				return Math.Asin(Convert.ToDouble(args[0]));
			}),
			["Acos"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Acos() expects 1 argument");
				if (!Misc.IsNumber(args[0]!))
					throw new RuntimeError("Acos() expects a number.");

				return Math.Acos(Convert.ToDouble(args[0]));
			}),
			["Atan"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Atan() expects 1 argument");
				if (!Misc.IsNumber(args[0]!))
					throw new RuntimeError("Atan() expects a number.");

				return Math.Atan(Convert.ToDouble(args[0]));
			}),
			["Asinh"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Asinh() expects 1 argument");
				if (!Misc.IsNumber(args[0]!))
					throw new RuntimeError("Asinh() expects a number.");

				return Math.Asinh(Convert.ToDouble(args[0]));
			}),
			["Acosh"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Acosh() expects 1 argument");
				if (!Misc.IsNumber(args[0]!))
					throw new RuntimeError("Acosh() expects a number.");

				return Math.Acosh(Convert.ToDouble(args[0]));
			}),
			["Atanh"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Atanh() expects 1 argument");
				if (!Misc.IsNumber(args[0]!))
					throw new RuntimeError("Atanh() expects a number.");

				return Math.Atanh(Convert.ToDouble(args[0]));
			}),
			["Atan2"] = new InternalFunction(args => {
				if (args.Length != 2) throw new RuntimeError("Atan2() expects 2 arguments");
				if (!Misc.IsNumber(args[0]!) || !Misc.IsNumber(args[1]!))
					throw new RuntimeError("Atan2() expects 2 numbers.");

				return Math.Atan2(Convert.ToDouble(args[0]), Convert.ToDouble(args[1]));
			}),
			["Abs"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Abs() expects 1 argument");
				if (!Misc.IsNumber(args[0]!))
					throw new RuntimeError("Abs() expects a number.");

				return Math.Abs(Convert.ToDouble(args[0]));
			}),
			["Floor"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Floor() expects 1 argument");
				if (!Misc.IsNumber(args[0]!))
					throw new RuntimeError("Floor() expects a number.");

				return Math.Floor(Convert.ToDouble(args[0]));
			}),
			["Ceil"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Ceil() expects 1 argument");
				if (!Misc.IsNumber(args[0]!))
					throw new RuntimeError("Ceil() expects a number.");

				return Math.Ceiling(Convert.ToDouble(args[0]));
			}),
			["Round"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Round() expects 1 argument");
				if (!Misc.IsNumber(args[0]!))
					throw new RuntimeError("Round() expects a number.");

				return Math.Round(Convert.ToDouble(args[0]));
			}),
			["Exp"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Exp() expects 1 argument");
				if (!Misc.IsNumber(args[0]!))
					throw new RuntimeError("Exp() expects a number.");

				return Math.Exp(Convert.ToDouble(args[0]));
			}),
			["Pow"] = new InternalFunction(args => {
				if (args.Length != 2) throw new RuntimeError("Pow() expects 2 arguments");
				if (!Misc.IsNumber(args[0]!) || !Misc.IsNumber(args[1]!))
					throw new RuntimeError("Pow() expects 2 numbers.");

				return Math.Pow(Convert.ToDouble(args[0]), Convert.ToDouble(args[1]));
			}),
			["Lerp"] = new InternalFunction(args => {
				if (args.Length != 2) throw new RuntimeError("Lerp(a, b, t) expects 3 arguments");
				if (!Misc.IsNumber(args[0]!) || !Misc.IsNumber(args[1]!) || !Misc.IsNumber(args[2]!))
					throw new RuntimeError("Lerp(a, b, t) expects 3 numbers.");

				var a = Convert.ToDouble(args[0]);
				var b = Convert.ToDouble(args[1]);
				var t = Convert.ToDouble(args[2]);

				return a + (b - a) / t;
			}),
			["Sqrt"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("Sqrt() expects 1 argument");
				if (!Misc.IsNumber(args[0]!))
					throw new RuntimeError("Sqrt() expects a number.");

				return Math.Sqrt(Convert.ToDouble(args[0]));
			}),
		});
	}
}