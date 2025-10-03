namespace Cranberry.PluginContracts;

// Shared contract assembly (Cranberry.PluginContracts.dll)
public interface IHost {
	// Register a function callable from Cranberry by name
	// e.g. host.RegisterFunction("raylib.InitWindow", (args) => {...});
	void RegisterFunction(string name, Func<object?[], object?> func);

	// Register an object (table) exposing members
	void RegisterObject(string name, IDictionary<string, object?> obj);

	// Utility: convert from CLR to Cranberry and vice versa if needed
	object? ToCranberry(object? clrValue);
	object? FromCranberry(object? cranberryValue);

	// Logging, diagnostics
	void Log(string message);
}

public interface ICranberryPlugin {
	// Called by host after loading plugin
	void Register(IHost host);

	// Optional: called before unloading
	void Unregister();
}