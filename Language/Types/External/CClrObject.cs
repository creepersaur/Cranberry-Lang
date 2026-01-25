using System.Reflection;
using Cranberry.External;
using Cranberry.Builtin;
using Cranberry.Errors;

namespace Cranberry.Types;

// A CLR-backed object visible to Cranberry. Implements IMemberAccessible so '.' works.
// Now also implements IExternalObject so ConvertCLR can detect / unwrap it.
public class CClrObject(object? instance, Type? type = null) : IMemberAccessible, IExternalObject {
	public readonly object? Instance = instance; // null for static types
	public readonly Type Type = type ?? instance?.GetType() ?? throw new ArgumentNullException(nameof(type));

	public object Internal => Instance!;

	private static object WrapClr(object? val) {
		if (val == null) return null!;
		var type = val.GetType();

		// convert primitives & strings to Cranberry types
		if (type.IsPrimitive || val is string)
			return ConvertCLR.ToCranberry(val)!;

		// wrap structs/classes
		return new CClrObject(val);
	}

	public object? ConstructNew(object[] args) {
		return Activator.CreateInstance(Type, args);
	}

	// Called for `obj.member`
	public object GetMember(object? member) {
		if (member is not string name)
			throw new RuntimeError($"CLR object member must be accessed with a string key, got {member}");

		// 1) Try property getter
		var prop = Type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		if (prop != null && prop.GetMethod != null) {
			try {
				var val = prop.GetValue(Instance);
				return WrapClr(val);
			} catch (TargetInvocationException tie) {
				throw tie.InnerException ?? tie;
			}
		}

		// 2) Try field
		var field = Type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		if (field != null) {
			var val = field.GetValue(Instance);
			return WrapClr(val);
		}

		// 3) Try methods: return an InternalFunction bound to this instance
		var methods = Type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase))
			.ToArray();

		if (methods.Length > 0) {
			// Return an InternalFunction that will attempt overload resolution and invoke on Instance
			return new InternalFunction((_, callArgs) => {
				// callArgs is a CLR array of Cranberry runtime values (not converted yet)
				// We'll try overloads and use ExternalManager.ConvertToClrPublic for conversions
				var errors = new System.Text.StringBuilder();

				foreach (var mi in methods) {
					var pars = mi.GetParameters();
					if (pars.Length != (callArgs?.Length ?? 0)) {
						errors.AppendLine($"Skipping {mi}: expects {pars.Length} args");
						continue;
					}

					var invokeArgs = new object[pars.Length];
					var ok = true;
					for (int i = 0; i < pars.Length; i++) {
						try {
							var conv = ExternalManager.ConvertToClr(callArgs![i], pars[i].ParameterType);
							// handle nullable value types
							if (conv == null && pars[i].ParameterType.IsValueType)
								invokeArgs[i] = Activator.CreateInstance(pars[i].ParameterType)!;
							else invokeArgs[i] = conv!;
						} catch (Exception ex) {
							ok = false;
							errors.AppendLine($"arg[{i}] -> {pars[i].ParameterType.Name} failed: {ex.Message}");
							break;
						}
					}

					if (!ok) continue;

					try {
						var result = mi.Invoke(Instance, invokeArgs);
						return WrapClr(result);
					} catch (TargetInvocationException tie) {
						throw tie.InnerException ?? tie;
					} catch (Exception invEx) {
						errors.AppendLine($"Invocation of {mi} failed: {invEx.Message}");
					}
				}

				throw new RuntimeError($"No matching instance overload for {Type.FullName}.{name} with {callArgs?.Length ?? 0} args.\n{errors}");
			}, methods[0]);
		}

		// Not found
		throw new RuntimeError($"Member `{name}` not found on CLR type `{Type.FullName}`.");
	}

	// For setting members: allow setting public fields/properties
	public void SetMember(object? member, object? value) {
		if (member is not string name) throw new RuntimeError("Member must be a string.");
		var prop = Type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		if (prop != null && prop.SetMethod != null) {
			var conv = ExternalManager.ConvertToClr(value, prop.PropertyType);
			prop.SetValue(Instance, conv);
			return;
		}

		var field = Type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		if (field != null) {
			var conv = ExternalManager.ConvertToClr(value, field.FieldType);
			field.SetValue(Instance, conv);
			return;
		}

		throw new RuntimeError($"Member `{name}` not found or not writable on CLR type `{Type.FullName}`.");
	}

	public override string ToString() => $"CLRObject({Type.FullName})";
}