using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Types;

namespace Cranberry.Namespaces;

public class N_Env : CNamespace {
	public N_Env(bool is_build) : base("Env", true) {
		var table = new Dictionary<string, object>();

		// registration helpers
		void AddConst(string name, object value) => table[name] = value;

		AddConst("args",
			is_build
				? new CList(Environment.GetCommandLineArgs().Select(object (x) => new CString(x)).ToList())
				: new CList(Environment.GetCommandLineArgs().Select(object (x) => new CString(x)).Skip(2).ToList())
		);


		// push dictionary into the env
		env.Variables.Push(table);
	}
}