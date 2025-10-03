namespace Cranberry.PluginContracts;

using System.Runtime.Loader;
using System.Reflection;

public class PluginLoadContext : AssemblyLoadContext {
	private readonly AssemblyDependencyResolver _resolver;

	public PluginLoadContext(string pluginPath) : base(true) // isCollectible = true
	{
		_resolver = new AssemblyDependencyResolver(pluginPath);
	}

	protected override Assembly? Load(AssemblyName assemblyName) {
		string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
		if (assemblyPath != null) return LoadFromAssemblyPath(assemblyPath);
		return null;
	}

	protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) {
		string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
		if (libraryPath != null) return LoadUnmanagedDllFromPath(libraryPath);
		return IntPtr.Zero;
	}
}

public class PluginManager {
	private readonly IHost _host; // your host implementation that implements IHost
	private readonly Dictionary<string, (PluginLoadContext, ICranberryPlugin)> _loaded = new();

	public PluginManager(IHost host) {
		_host = host;
	}

	public void LoadPlugin(string pluginDllPath) {
		var alc = new PluginLoadContext(pluginDllPath);
		var asm = alc.LoadFromAssemblyPath(pluginDllPath);

		// find types implementing ICranberryPlugin
		var pluginType = asm.GetTypes().FirstOrDefault(t => typeof(ICranberryPlugin).IsAssignableFrom(t) && !t.IsAbstract);
		if (pluginType == null)
			throw new InvalidOperationException("No ICranberryPlugin implementation found.");

		var plugin = (ICranberryPlugin)Activator.CreateInstance(pluginType)!;

		plugin.Register(_host);
		_loaded[pluginDllPath] = (alc, plugin);
		_host.Log($"Loaded plugin {pluginDllPath} -> {pluginType.FullName}");
	}

	public void UnloadPlugin(string pluginDllPath) {
		if (!_loaded.TryGetValue(pluginDllPath, out var tuple)) return;
		tuple.Item2.Unregister();
		tuple.Item1.Unload();
		_loaded.Remove(pluginDllPath);
		GC.Collect();
		GC.WaitForPendingFinalizers();
		_host.Log($"Unloaded {pluginDllPath}");
	}
}