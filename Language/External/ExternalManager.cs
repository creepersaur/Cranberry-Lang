using System.Reflection;
using System.Collections.Concurrent;
using Cranberry.Types;
using Cranberry.Nodes;
using Cranberry.Builtin;
using System.Runtime.InteropServices;

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

		// 1) Collect all candidates by name
		List<MethodInfo> allCandidates = new();

		if (!string.IsNullOrEmpty(typeName)) {
			var type = asm.GetType(typeName, throwOnError: true);
			allCandidates.AddRange(
				type!.GetMethods(BindingFlags.Public | BindingFlags.Static)
					.Where(m => string.Equals(m.Name, functionName, StringComparison.OrdinalIgnoreCase))
			);
		} else {
			foreach (var t in asm.GetExportedTypes()) {
				allCandidates.AddRange(
					t.GetMethods(BindingFlags.Public | BindingFlags.Static)
						.Where(m => string.Equals(m.Name, functionName, StringComparison.OrdinalIgnoreCase))
				);
			}
		}

		if (allCandidates.Count == 0)
			throw new InvalidOperationException(
				$"Method '{functionName}' not found as a public static in any exported type of {modulePath}."
			);

		// 2) Prefer managed overloads
		MethodInfo[] managed = allCandidates
			.Where(m =>
				!m.IsDefined(typeof(DllImportAttribute), false)
			)
			.ToArray();

		MethodInfo selected;

		if (managed.Length == 1) {
			selected = managed[0];
		} else if (managed.Length > 1) {
			// Multiple managed overloads → ambiguous
			throw new InvalidOperationException(
				$"Multiple managed overloads found for '{functionName}'. " +
				$"Disambiguate using typeName.\n" +
				string.Join("\n", managed.Select(m => "  " + m))
			);
		} else {
			// 3) Fallback: allow pointer/native overloads
			var native = allCandidates.ToArray();

			if (native.Length == 1) {
				selected = native[0];
			} else {
				throw new InvalidOperationException(
					$"No managed overload found for '{functionName}', and multiple native overloads exist. " +
					$"Disambiguate using typeName.\n" +
					string.Join("\n", native.Select(m => "  " + m))
				);
			}
		}

		// 4) Build wrapper
		object Wrapper(object[] args) {
			var pars = selected.GetParameters();
			if (pars.Length != args.Length)
				throw new ArgumentException(
					$"Wrong argument count for {functionName}. Expected {pars.Length} but got {args.Length}."
				);

			object[] invokeArgs = new object[args.Length];
			for (int i = 0; i < args.Length; i++) {
				invokeArgs[i] = ConvertToClr(args[i], pars[i].ParameterType)!;
			}

			return selected.Invoke(null, invokeArgs)!;
		}

		// 5) Register
		var key = MakeKey(modulePath, functionName);
		_externs[key] = Wrapper;
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

		if (cranVal is IExternalObject ext)
			return ext.Internal;

		if (cranVal is CClrObject clrObj) {
			// Check if the wrapped type matches or is assignable to target
			if (targetType.IsAssignableFrom(clrObj.Type)) {
				return clrObj.Instance;  // Return the actual CLR object
			}

			// Try to convert the wrapped instance
			return Convert.ChangeType(clrObj.Instance, targetType);
		}

		if (cranVal is InternalFunction func) {
			// If the caller expects a delegate, create one
			if (typeof(Delegate).IsAssignableFrom(targetType)
				&& func.InternalMethod is MethodInfo mi) {

				return Delegate.CreateDelegate(
					targetType,
					mi.IsStatic ? null : (func.InternalTarget ?? (mi.DeclaringType!.IsValueType ? null : null)),
					mi
				);
			}

			return func.InternalMethod;
		}

		// If already compatible:
		if (targetType.IsInstanceOfType(cranVal)) return cranVal;

		return cranVal switch {
			// If cranVal is your runtime wrapper types (CString, CNumber, etc.), unwrap them here.
			// Replace these checks with your actual runtime types.
			NullNode => null,

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

	public static Dictionary<string, Func<object[], object>> RegisterAllManagedFunctionsFromAssembly(string modulePath) {
		if (!File.Exists(modulePath))
			throw new FileNotFoundException($"DLL not found: {modulePath}");

		var asm = Assembly.LoadFrom(modulePath);

		// group methods by simple name (overloads will be a list)
		var methodGroups = new Dictionary<string, List<MethodInfo>>(StringComparer.OrdinalIgnoreCase);

		foreach (var t in asm.GetExportedTypes()) {
			// only public types
			if (!t.IsPublic) continue;

			foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
				// skip special names / property accessors
				if (m.IsSpecialName) continue;

				if (!methodGroups.TryGetValue(m.Name, out var list)) {
					list = new List<MethodInfo>();
					methodGroups[m.Name] = list;
				}
				list.Add(m);
			}
		}

		var wrappers = new Dictionary<string, Func<object[], object>>(StringComparer.OrdinalIgnoreCase);

		foreach (var kv in methodGroups) {
			var methodName = kv.Key;
			var overloads = kv.Value.ToArray();

			// Create a wrapper which will pick the best overload at call time
			object Wrapper(object[] args) {
				// Try to find an overload that matches parameter count and convertible args
				foreach (var mi in overloads) {
					var pars = mi.GetParameters();
					if (pars.Length != args.Length) continue;

					object[] invokeArgs = new object[args.Length];
					bool ok = true;

					for (int i = 0; i < args.Length; i++) {
						try {
							// Reuse your ConvertToClr logic (private method). We call it through reflection-like approach
							var converted = ConvertToClr(args[i], pars[i].ParameterType);
							invokeArgs[i] = converted!;
						} catch {
							ok = false;
							break;
						}
					}

					if (!ok) continue;

					try {
						var result = mi.Invoke(null, invokeArgs);
						return result!;
					} catch (TargetInvocationException) {
						// If the target threw, bubble useful info up.
						throw;
					} catch {
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
