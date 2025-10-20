using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;
using Cranberry.Types;

namespace Cranberry.Namespaces;

public class N_IO : CNamespace {
	public N_IO() : base("IO", true) {
		env.Variables.Push(new Dictionary<string, object> {
			["read_line"] = new InternalFunction((_, args) => {
				if (args.Length > 0) {
					string prompt = "";
					foreach (var v in args) {
						prompt += $"{v} ";
					}

					Console.Write(prompt[..^1]);
				}

				return new CString(Console.ReadLine()!);
			}),

			["read"] = new InternalFunction((_, args) => {
				if (args.Length > 0) {
					string prompt = "";
					foreach (var v in args) {
						prompt += $"{v} ";
					}

					Console.Write(prompt[..^1]);
				}

				return (double)Console.Read();
			}),

			["read_key"] = new InternalFunction((_, args) => {
				if (args.Length > 1)
					throw new RuntimeError("ReadKey(intercept?) takes in 1 argument.");

				ConsoleKeyInfo keyInfo;
				if (args.Length == 1 && Misc.IsTruthy(args[0])) {
					keyInfo = Console.ReadKey(true);
				} else {
					keyInfo = Console.ReadKey();
				}

				// If it's a printable character, return that character.
				if (keyInfo.KeyChar != '\0') {
					return new CString(keyInfo.KeyChar.ToString());
				}

				// Otherwise return the ConsoleKey name, including modifiers if any.
				string result = keyInfo.Key.ToString(); // e.g. "RightArrow", "F1", "Enter"
				if (keyInfo.Modifiers != 0) {
					result = $"{keyInfo.Modifiers}+{result}"; // e.g. "Control+LeftArrow"
				}

				return new CString(result);
			}),

			["clear"] = new InternalFunction((_, args) => {
				if (args.Length != 0)
					throw new RuntimeError("clear() takes 0 arguments.");

				Console.Clear();
				return new NullNode();
			}),

			["beep"] = new InternalFunction((_, args) => {
				if (args.Length == 1 || args.Length > 2)
					throw new RuntimeError("beep(frequency, duration) takes 0 or 2 arguments.");

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

			["write"] = new InternalFunction((_, args) => {
				if (args.Length > 0) {
					string prompt = "";
					foreach (var v in args) {
						prompt += $"{v} ";
					}

					Console.Write(prompt[..^1]);
				}

				return new NullNode();
			}),

			["write_line"] = new InternalFunction((_, args) => {
				if (args.Length > 0) {
					string prompt = "";
					foreach (var v in args) {
						prompt += $"{v} ";
					}

					Console.WriteLine(prompt[..^1]);
				}

				return new NullNode();
			}),

			["set_foreground"] = new InternalFunction((_, args) => {
				if (args.Length != 1) throw new RuntimeError("`set_foreground(color: string)` expects 1 argument.");
      
				if (args[0] is CString colorName) {
					try {
						Console.ForegroundColor = Enum.Parse<ConsoleColor>(colorName.Value, ignoreCase: true);
					} catch (ArgumentException) {
						throw new RuntimeError($"Invalid color name: '{colorName.Value}'. Valid colors are: {string.Join(", ", Enum.GetNames<ConsoleColor>())}");
					}
				} else if (args[0] is double colorValue) {
					Console.ForegroundColor = (ConsoleColor)(int)colorValue;
				} else {
					throw new RuntimeError("`set_foreground` expects a string color name or number.");
				}
				return new NullNode();
			}),

			["reset_color"] = new InternalFunction((_, _) => {
				Console.ResetColor();
				return new NullNode();
			}),
		});
	}
}