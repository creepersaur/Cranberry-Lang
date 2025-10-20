using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;
using Cranberry.Types;

namespace Cranberry.Language.Builtin;

public abstract class RegisterBuiltin {
	public static void Register(Interpreter interpreter, Env env) {
		env.Define("print", new InternalFunction((_, args) => BuiltinInternal.Print(args)));
		env.Define("println", new InternalFunction((_, args) => BuiltinInternal.Print(args, true)));
		env.Define("error", new InternalFunction((start_token, args) => BuiltinInternal.Error(start_token!, args)));
		env.Define("format", new InternalFunction((_, args) => BuiltinInternal.Format(args)));
		
		// Actually CastNode parsed internally, show function externally
		env.Define("number", new InternalFunction((_, _) => null));
		env.Define("string", new InternalFunction((_, _) => null));
		env.Define("bool", new InternalFunction((_, _) => null));
		env.Define("char", new InternalFunction((_, _) => null));
		env.Define("list", new InternalFunction((_, _) => null));
		
		env.Define("Dict", new InternalFunction((_, _) => new CDict(new Dictionary<object, object>())));
		env.Define("List", new InternalFunction((_, args) => {
			if (args.Length == 1) {
				return new CList([args[0]!]);
			}

			if (args is [_, double d]) return new CList(new object[Convert.ToInt32(d)].Select(_ => args[0]!).ToList());

			if (args.Length == 0)
				return new CList([]);

			throw new RuntimeError("List() got invalid arguments. (It can take no arguments, a value, and optional size amount.)");
		}));
		
		env.Define("typeof", new InternalFunction((_, args) => {
			if (args.Length < 1)
				throw new RuntimeError("`typeof(obj, internal?)` expects at least 1 argument.");

			return BuiltinInternal.Typeof(args);
		}));
		env.Define("pcall", new InternalFunction((_, args) => {
			if (args.Length < 1)
				throw new RuntimeError("`pcall(fn, ...)` expects at least one argument. (Function to be called)");

			var list_args = args.ToList();
			var func = list_args[0];
			list_args.RemoveAt(0);

			if (func is FunctionNode f) {
				try {
					return new CList([
						true, interpreter.Evaluate(new FunctionCall(null, "", args) {
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