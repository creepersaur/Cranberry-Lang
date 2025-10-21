using System.Reflection;
using System.Collections.Concurrent;
using Cranberry.Types;

namespace Cranberry.External;

// ---------- ExternalManager: loads methods from managed DLLs ----------
public static class ExternalManager {
	// key: modulePath::functionName  (or just functionName if you prefer)
	private static readonly ConcurrentDictionary<string, Func<object[], object>> _externs
		= new(StringComparer.OrdinalIgnoreCase);

	// Public call to load and register a managed static method from a dll
	// modulePath: path to Something.dll (can be relative)
	// functionName: the function symbol you want to expose to Cranberry (e.g. "Hello")
	// optional: typeName - if provided, only search that type; otherwise search all public types for a matching static method name
	public static void RegisterManagedFunctionFromAssembly(string modulePath, string functionName, string? typeName = null) {
		if (!File.Exists(modulePath))
			throw new FileNotFoundException($"DLL not found: {modulePath}");

		// try load the assembly (will throw if not a .NET assembly)
		var asm = Assembly.LoadFrom(modulePath);

		// find candidate MethodInfo(s)
		MethodInfo? method = null;
		if (!string.IsNullOrEmpty(typeName)) {
			var type = asm.GetType(typeName, throwOnError: true);
			method = type!.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.FirstOrDefault(m => string.Equals(m.Name, functionName, StringComparison.OrdinalIgnoreCase));
		} else {
			// search types for a public static method with that name
			foreach (var t in asm.GetExportedTypes()) {
				var mi = t.GetMethods(BindingFlags.Public | BindingFlags.Static)
					.FirstOrDefault(m => string.Equals(m.Name, functionName, StringComparison.OrdinalIgnoreCase));
				if (mi != null) {
					method = mi;
					break;
				}
			}
		}

		if (method == null)
			throw new InvalidOperationException($"Method '{functionName}' not found as a public static in any exported type of {modulePath}.");

		var key = MakeKey(modulePath, functionName);
		_externs[key] = Wrapper;
		return;

		// Build wrapper that accepts object[] args and returns object
		object Wrapper(object[] args) {
			var pars = method.GetParameters();
			if (pars.Length != args.Length) throw new ArgumentException($"Wrong argument count for {functionName}. Expected {pars.Length} but got {args.Length}.");

			// convert each argument to the parameter type (naive)
			object[] invokeArgs = new object[args.Length];
			for (int i = 0; i < args.Length; i++) {
				var targetType = pars[i].ParameterType;
				invokeArgs[i] = ConvertToClr(args[i], targetType)!;
			}

			// If method belongs to a static generic type/method, reflection can be more complex; this handles the common case.
			var result = method.Invoke(null, invokeArgs);
			return result!;
		}
	}

	public static bool TryResolve(string modulePath, string functionName, out Func<object[], object>? func) {
		var key = MakeKey(modulePath, functionName);
		if (_externs.TryGetValue(key, out func)) return true;

		// also try a key with just functionName (optional allowance)
		if (_externs.TryGetValue(functionName, out func)) return true;

		func = null;
		return false;
	}

	private static string MakeKey(string modulePath, string functionName)
		=> $"{modulePath}::{functionName}";

	// VERY naive conversions - adapt to your runtime types
	public static object? ConvertToClr(object? cranVal, Type targetType) {
		if (cranVal == null) return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

		if (cranVal is CClrObject clrObj) {
			// Check if the wrapped type matches or is assignable to target
			if (targetType.IsAssignableFrom(clrObj.Type)) {
				return clrObj.Instance;  // Return the actual CLR object
			}

			// Try to convert the wrapped instance
			return Convert.ChangeType(clrObj.Instance, targetType);
		}

		// If already compatible:
		if (targetType.IsInstanceOfType(cranVal)) return cranVal;

		return cranVal switch {
			// If cranVal is your runtime wrapper types (CString, CNumber, etc.), unwrap them here.
			// Replace these checks with your actual runtime types.
			string s when targetType == typeof(string) || targetType == typeof(object) => s,
			CString s when targetType == typeof(string) || targetType == typeof(object) => s.Value,

			// suppose your numeric runtime is double/float
			double d when targetType == typeof(int) => Convert.ToInt32(d),
			double d when targetType == typeof(long) => Convert.ToInt64(d),
			double d when targetType == typeof(float) => Convert.ToSingle(d),
			double d when targetType == typeof(double) => d,
			double d when targetType == typeof(object) => d,

			_ => Convert.ChangeType(cranVal, targetType)
		};

		// fallback: try System.Convert
	}

	public static Dictionary<string, Func<object[], object>> RegisterAllManagedFunctionsFromAssembly(string modulePath)
    {
        if (!File.Exists(modulePath))
            throw new FileNotFoundException($"DLL not found: {modulePath}");

        var asm = Assembly.LoadFrom(modulePath);

        // group methods by simple name (overloads will be a list)
        var methodGroups = new Dictionary<string, List<MethodInfo>>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in asm.GetExportedTypes())
        {
            // only public types
            if (!t.IsPublic) continue;

            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                // skip special names / property accessors
                if (m.IsSpecialName) continue;

                if (!methodGroups.TryGetValue(m.Name, out var list))
                {
                    list = new List<MethodInfo>();
                    methodGroups[m.Name] = list;
                }
                list.Add(m);
            }
        }

        var wrappers = new Dictionary<string, Func<object[], object>>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in methodGroups)
        {
            var methodName = kv.Key;
            var overloads = kv.Value.ToArray();

            // Create a wrapper which will pick the best overload at call time
            object Wrapper(object[] args)
            {
				// Try to find an overload that matches parameter count and convertible args
				foreach (var mi in overloads)
				{
					var pars = mi.GetParameters();
					if (pars.Length != args.Length) continue;

					object[] invokeArgs = new object[args.Length];
					bool ok = true;

					for (int i = 0; i < args.Length; i++)
					{
						try
						{
							// Reuse your ConvertToClr logic (private method). We call it through reflection-like approach
							var converted = ConvertToClr(args[i], pars[i].ParameterType);
							invokeArgs[i] = converted!;
						}
						catch
						{
							ok = false;
							break;
						}
					}

					if (!ok) continue;

					try
					{
						var result = mi.Invoke(null, invokeArgs);
						return result!;
					}
					catch (TargetInvocationException)
					{
						// If the target threw, bubble useful info up.
						throw;
					}
					catch
					{
						// Ignore
					}
				}

                // If we got here, no overload matched
				// foreach (var i in args) {
				// 	Console.WriteLine("Argument `{0}`: {1}", i, i.GetType());
				// }
                throw new ArgumentException($"No matching overload found for {methodName} with {args.Length} arguments.");
            }

            // register wrapper in our runtime dictionary (module-specific keys handled by RegisterManagedFunctionFromAssembly style)
            var key = MakeKey(modulePath, methodName);
            _externs[key] = Wrapper;
            // also register by bare name so scripts that just call MethodName(...) can resolve (optional)
            if (!_externs.ContainsKey(methodName))
                _externs[methodName] = Wrapper;

            wrappers[methodName] = Wrapper;
        }

        return wrappers;
    }
}
