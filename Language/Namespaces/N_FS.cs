using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Types;

namespace Cranberry.Namespaces;

public class N_FS : CNamespace {
	public N_FS() : base("FS", true) {
		env.Variables.Push(new Dictionary<string, object> {
			["GetFile"] = new InternalFunction(args => {
				if (args.Length != 1)
					throw new RuntimeError("`GetFile(path)` expects 1 argument.");

				if (args[0] is CString path)
					return new CFile(path.Value);

				throw new RuntimeError("`GetFile(path)` expects a `string` path.");
			}),
			
			["GetDirectory"] = new InternalFunction(args => {
				if (args.Length != 1)
					throw new RuntimeError("`GetDirectory(path)` expects 1 argument.");

				if (args[0] is CString path)
					return new CDirectory(path.Value);

				throw new RuntimeError("`GetDirectory(path)` expects a `string` path.");
			})
		});
	}
}