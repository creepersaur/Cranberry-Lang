using System.Reflection;
using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.External;
using Cranberry.Nodes;

namespace Cranberry.Types {
	// A callable type object: e.g. Raylib.Color in the namespace.
	// It is an InternalFunction (so it can be called) and IMemberAccessible so you can do Color.RAYWHITE
	public class CClrType : InternalFunction, IMemberAccessible {
		public readonly Type ClrType;

		// factoryFunc: called with Cranberry args (raw runtime values), return value must be a Cranberry value (e.g. CClrObject)
		public CClrType(Type type, Func<Token?, object?[], object?> factoryFunc) : base(factoryFunc) {
			ClrType = type;
		}

		private static object WrapValue(object? val) {
			if (val == null) return new NullNode();

			var type = val.GetType();

			// Convert primitives to Cranberry types
			if (type == typeof(bool)) return val;
			if (type == typeof(string)) return new CString((string)val);
			if (type.IsPrimitive) return Convert.ToDouble(val);

			// For structs/classes (like Color), wrap them so reflection continues to work
			return new CClrObject(val);
		}

		public object GetMember(object? member) {
			if (member is not string name) throw new RuntimeError("Type member must be a string.");

			// 1) Static field
			var field = ClrType.GetField(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
			if (field != null) {
				var val = field.GetValue(null);
				return WrapValue(val); // Changed this line
			}

			// 2) Static property
			var prop = ClrType.GetProperty(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
			if (prop != null && prop.GetMethod != null) {
				var val = prop.GetValue(null);
				return WrapValue(val); // Changed this line
			}

			// 3) Static methods -> return callable InternalFunction (handles overloads)
			var methods = ClrType.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Where(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase))
				.ToArray();
			if (methods.Length > 0) {
				return new InternalFunction((_, callArgs) => {
					// Overload resolution + conversion (same approach you already use)
					var errors = new System.Text.StringBuilder();
					foreach (var mi in methods) {
						var pars = mi.GetParameters();
						if (pars.Length != (callArgs?.Length ?? 0)) {
							errors.AppendLine($"skip {mi}: expects {pars.Length}");
							continue;
						}

						var invokeArgs = new object[pars.Length];
						var ok = true;
						for (int i = 0; i < pars.Length; i++) {
							try {
								invokeArgs[i] = ExternalManager.ConvertToClr(callArgs![i], pars[i].ParameterType)!;
							} catch (Exception ex) {
								ok = false;
								errors.AppendLine(ex.Message);
								break;
							}
						}

						if (!ok) continue;

						try {
							var res = mi.Invoke(null, invokeArgs);
							return ConvertCLR.ToCranberry(res)!;
						} catch (TargetInvocationException tie) {
							throw tie.InnerException ?? tie;
						} catch (Exception ex) {
							errors.AppendLine(ex.Message);
						}
					}

					throw new RuntimeError($"No matching static overload for {ClrType.FullName}.{name} with {(callArgs?.Length ?? 0)} args.\n{errors}");
				});
			}

			throw new RuntimeError($"Static member `{name}` not found on type `{ClrType.FullName}`.");
		}

		public void SetMember(object? member, object? value) {
			if (member is not string name) throw new RuntimeError("Type member must be a string.");
			// write to static field/property if writable
			var field = ClrType.GetField(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
			if (field != null) {
				var conv = ExternalManager.ConvertToClr(value, field.FieldType);
				field.SetValue(null, conv);
				return;
			}

			var prop = ClrType.GetProperty(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
			if (prop != null && prop.SetMethod != null) {
				var conv = ExternalManager.ConvertToClr(value, prop.PropertyType);
				prop.SetValue(null, conv);
				return;
			}

			throw new RuntimeError($"Static writable member `{name}` not found on type `{ClrType.FullName}`.");
		}

		public override string ToString() => $"CLRType:{ClrType.FullName}";
	}
}