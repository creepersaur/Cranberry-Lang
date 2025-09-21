using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Types;

namespace Cranberry.Namespaces;

public class N_FS : CNamespace {
	public N_FS() : base("FS", true) {
		env.Variables.Push(new Dictionary<string, object> {
			//////////////////////////////////////////////////////////
			// METHODS
			//////////////////////////////////////////////////////////

			{
				"OpenFile", new InternalFunction(args => {
					if (args.Length != 1)
						throw new RuntimeError("`OpenFile(path)` expects 1 argument.");

					if (args[0] is string path)
						return new CFile(path);

					throw new RuntimeError("`OpenFile(path)` expects a `string` path.");
				})
			}
		});
	}
}