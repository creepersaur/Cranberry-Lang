using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;
using Cranberry.Types;

namespace Cranberry.Language.Builtin;

public abstract class RegisterBuiltin {
	public static void Register(Interpreter interpreter, Env env) {
		env.Define("print", new InternalFunction(args => BuiltinInternal.Print(args)));
		env.Define("println", new InternalFunction(args => BuiltinInternal.Print(args, true)));
		env.Define("number", new InternalFunction(args => {
			if (args.Length != 1)
				throw new RuntimeError("`number()` expects 1 argument.");

			return BuiltinInternal.ToNumber(args[0]);
		}));
		env.Define("string", new InternalFunction(args => {
			if (args.Length != 1)
				throw new RuntimeError("`string()` expects 1 argument.");

			return BuiltinInternal.ToString(args[0]);
		}));
		env.Define("typeof", new InternalFunction(args => {
			if (args.Length < 1)
				throw new RuntimeError("`typeof(obj, internal?)` expects at least 1 argument.");

			return BuiltinInternal.Typeof(args);
		}));
		env.Define("format", new InternalFunction(BuiltinInternal.Format));
		env.Define("List", new InternalFunction(args => {
			if (args.Length == 1) {
				return new CList([args[0]!]);
			}

			if (args is [_, double d]) return new CList(new object[Convert.ToInt32(d)].Select(_ => args[0]!).ToList());

			if (args.Length == 0)
				return new CList([]);

			throw new RuntimeError("List() got invalid arguments. (It can take no arguments, a value, and optional size amount.)");
		}));
		env.Define("Dict", new InternalFunction(_ => new CDict(new Dictionary<object, object>())));
		env.Define("pcall", new InternalFunction(args => {
			if (args.Length < 1)
				throw new RuntimeError("`pcall(fn, ...)` expects at least one argument. (Function to be called)");

			var list_args = args.ToList();
			var func = list_args[0];
			list_args.RemoveAt(0);

			if (func is FunctionNode f) {
				try {
					return new CList([
						true, interpreter.Evaluate(new FunctionCall("", args) {
							Target = f
						})
					], true);
				} catch (Exception e) {
					return new CList([false, e.Message], true);
				}
			}

			if (func is InternalFunction i) {
				try {
					return new CList([true, i.Call(args!) ?? new NullNode()], true);
				} catch (Exception e) {
					return new CList([false, e.Message], true);
				}
			}

			throw new RuntimeError("`pcall(fn, ...)` expects a function as first argument.");
		}));
	}
}