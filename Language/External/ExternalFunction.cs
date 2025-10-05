namespace Cranberry.External;

// ---------- ExternFunction: a runtime value representing an external function ----------
public class ExternFunction(string modulePath, string functionName, Func<object[], object> wrapper, int arity) {
	// wrapper takes raw CLR args array and returns CLR object
	public readonly string ModulePath = modulePath;
	public readonly string FunctionName = functionName;
	public readonly int Arity = arity;

	// call from interpreter; adapt signature to how you call FunctionNode currently
	public object? Invoke(object[] args)
	{
		// note: args here are *already* converted from Cranberry -> CLR types in many designs;
		// here we expect interpreter to pass raw primitive/CLR values for simplicity.
		var result = wrapper(args);
		return result;
	}
}
