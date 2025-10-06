using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;
using Cranberry.Types;

namespace Cranberry.Namespaces;

public class N_IO : CNamespace {
	public N_IO() : base("IO", true) {
		env.Variables.Push(new Dictionary<string, object> {
			["ReadLine"] = new InternalFunction(args => {
				if (args.Length > 0) {
					string prompt = "";
					foreach (var v in args) {
						prompt += $"{v} ";
					}

					Console.Write(prompt[..^1]);
				}

				return new CString(Console.ReadLine()!);
			}),

			["Read"] = new InternalFunction(args => {
				if (args.Length > 0) {
					string prompt = "";
					foreach (var v in args) {
						prompt += $"{v} ";
					}

					Console.Write(prompt[..^1]);
				}

				return (double)Console.Read();
			}),

			["ReadKey"] = new InternalFunction(args => {
				if (args.Length > 1)
					throw new RuntimeError("ReadKey(intercept?) takes in 1 argument.");

				ConsoleKeyInfo keyInfo;
				if (args.Length == 1 && Misc.IsTruthy(args[0])) {
					keyInfo = Console.ReadKey(true);
				} else {
					keyInfo = Console.ReadKey();
				}

				if (char.IsControl(keyInfo.KeyChar) || keyInfo.Key != ConsoleKey.None) {
					return keyInfo.Key.ToString();
				}

				return new CString(keyInfo.KeyChar.ToString());
			}),

			["Clear"] = new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("Clear() takes 0 arguments.");

				Console.Clear();
				return new NullNode();
			}),

			["Beep"] = new InternalFunction(args => {
				if (args.Length == 1 || args.Length > 2)
					throw new RuntimeError("Beep(frequency, duration) takes 0 or 2 arguments.");

				if (args.Length == 2) {
					if (!Misc.IsNumber(args[0]!) || !Misc.IsNumber(args[1]!))
						throw new RuntimeError("Beep(frequency, duration) takes 0 or 2 INTEGERS.");

#pragma warning disable CA1416
					Console.Beep(Convert.ToInt32(args[0]!), Convert.ToInt32(args[1]!));
#pragma warning restore CA1416
				} else {
					Console.Beep();
				}

				return new NullNode();
			}),

			["Write"] = new InternalFunction(args => {
				if (args.Length > 0) {
					string prompt = "";
					foreach (var v in args) {
						prompt += $"{v} ";
					}

					Console.Write(prompt[..^1]);
				}

				return new NullNode();
			}),

			["WriteLine"] = new InternalFunction(args => {
				if (args.Length > 0) {
					string prompt = "";
					foreach (var v in args) {
						prompt += $"{v} ";
					}

					Console.WriteLine(prompt[..^1]);
				}

				return new NullNode();
			}),

			["SetForeground"] = new InternalFunction(args => {
				if (args.Length != 1) throw new RuntimeError("`SetForeground(color)` expects 1 argument.");
      
				if (args[0] is CString colorName) {
					try {
						Console.ForegroundColor = Enum.Parse<ConsoleColor>(colorName.Value, ignoreCase: true);
					} catch (ArgumentException) {
						throw new RuntimeError($"Invalid color name: '{colorName.Value}'. Valid colors are: {string.Join(", ", Enum.GetNames<ConsoleColor>())}");
					}
				} else if (args[0] is double colorValue) {
					Console.ForegroundColor = (ConsoleColor)(int)colorValue;
				} else {
					throw new RuntimeError("`SetForeground` expects a string color name or number.");
				}
				return new NullNode();
			}),

			["ResetColor"] = new InternalFunction(_ => {
				Console.ResetColor();
				return new NullNode();
			}),
		});
	}
}