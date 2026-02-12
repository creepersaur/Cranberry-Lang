using System.Collections;
using System.Reflection;
using Cranberry.Nodes;
using Cranberry.Types;

namespace Cranberry.External;

public static class ConvertCLR {
	public static object? ToClr(object obj) {
		// If this is a Cranberry wrapper exposing an Internal CLR instance, unwrap it.
		if (obj is IExternalObject ext)
			return ext.Internal;

		return obj switch {
			CString c => c.Value,
			double d => d,
			char c => c,
			string s => s,
			bool b => b,
			CList l => l.Items.Select(ToClr).ToArray(),
			CDict D => D.Items.Select(object (x) => new KeyValuePair<object?, object?>(ToClr(x.Key), ToClr(x.Value))).ToArray(),
			NullNode => null,
			_ => obj
		}; 
	}

	public static object? ToCranberry(object? obj) {
		switch (obj) {
			case null:
				return new NullNode();
			// Already a Cranberry runtime object
			case CString:
			case CList:
			case CDict:
				return obj;
			// Strings → CString
			case string s:
				return new CString(s);
			// Booleans and chars used as-is
			case bool b:
				return b;
			case char c:
				return c;
			// Numeric types → double
			case double dd:
				return dd;
			case float f:
				return Convert.ToDouble(f);
			case decimal dec:
				return Convert.ToDouble(dec);
			case int i:
				return Convert.ToDouble(i);
			case long l:
				return Convert.ToDouble(l);
			case short sh:
				return Convert.ToDouble(sh);
			case byte by:
				return Convert.ToDouble(by);
			case uint ui:
				return Convert.ToDouble(ui);
			case ulong ul:
				return Convert.ToDouble(ul);
			case ushort us:
				return Convert.ToDouble(us);
			case sbyte sb:
				return Convert.ToDouble(sb);
		}

		var type = obj.GetType();

		// 1) If object is IConvertible / primitive-like, use TypeCode to handle safely
		try {
			var tc = Convert.GetTypeCode(obj);
			switch (tc) {
				case TypeCode.Boolean: return Convert.ToBoolean(obj);
				case TypeCode.Char: return Convert.ToChar(obj);
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return Convert.ToDouble(obj);
				case TypeCode.String:
					return new CString(Convert.ToString(obj)!);
				case TypeCode.Object:
				case TypeCode.Empty:
				case TypeCode.DBNull:
				default:
					break;
			}
		} catch {
			// ignore — fall through to other strategies
		}

		// 2) Try implicit/explicit conversion operators defined on the type that produce a simple CLR type
		var conv = FindConversionOperatorReturningSimple(type);
		if (conv != null) {
			try {
				var convResult = conv.Invoke(null, [obj]);
				return ToCranberry(convResult);
			} catch {
				// ignore and continue
			}
		}

		// 3) If the type has exactly one public instance property or field that is a simple CLR type, unwrap it
		// if (TryGetSinglePrimitiveMemberValue(obj, out var singleVal)) {
		// 	return ToCranberry(singleVal);
		// }

		// 4) For types from 3rd-party libraries (like Raylib-cs), attempt to produce a dict of public members
		// var publicProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
		// 	.Where(p => p.GetMethod != null).ToArray();
		// var publicFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

		// if (publicProps.Length > 0 || publicFields.Length > 0) {
		// 	var dictPairs = new List<KeyValuePair<object, object>>();
		// 	foreach (var p in publicProps) {
		// 		object? val;
		// 		try {
		// 			val = p.GetValue(obj);
		// 		} catch {
		// 			val = null;
		// 		}

		// 		dictPairs.Add(new KeyValuePair<object, object>(p.Name, ToCranberry(val)!));
		// 	}

		// 	foreach (var f in publicFields) {
		// 		object? val;
		// 		try {
		// 			val = f.GetValue(obj);
		// 		} catch {
		// 			val = null;
		// 		}

		// 		// avoid duplicate keys if property with same name exists
		// 		if (!dictPairs.Any(kv => kv.Key.Equals(f.Name)))
		// 			dictPairs.Add(new KeyValuePair<object, object>(f.Name, ToCranberry(val)!));
		// 	}

		// 	// If we collected any members, return as CDict (struct-like)
		// 	if (dictPairs.Count > 0) return new CDict(dictPairs.ToDictionary());
		// }

		// 5) IDictionary (non-generic or generic) -> CDict
		if (obj is IDictionary dict) {
			var pairs = new List<KeyValuePair<object, object>>();
			foreach (DictionaryEntry e in dict) {
				pairs.Add(new KeyValuePair<object, object>(ToCranberry(e.Key)!, ToCranberry(e.Value)!));
			}

			return new CDict(pairs.ToDictionary());
		}

		// 6) KeyValuePair[] -> CDict
		if (type.IsArray && type.GetElementType()?.IsGenericType == true
		                 && type.GetElementType()!.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
			var pairs = new List<KeyValuePair<object, object>>();
			foreach (var item in (IEnumerable)obj) {
				var keyProp = item.GetType().GetProperty("Key")!;
				var valProp = item.GetType().GetProperty("Value")!;
				pairs.Add(new KeyValuePair<object, object>(
					ToCranberry(keyProp.GetValue(item))!,
					ToCranberry(valProp.GetValue(item))!
				));
			}

			return new CDict(pairs.ToDictionary());
		}

		// 7) IEnumerable -> CList
		if (obj is IEnumerable ie && !(obj is string)) {
			var items = new List<object>();
			foreach (var e in ie) items.Add(ToCranberry(e)!);
			return new CList(items);
		}

		// Fallback -> return original CLR object (host object)
		return new CClrObject(obj);
	}

	// Helpers --------------------------------------------------------------

	// Return true if the type is a simple CLR type we prefer to convert to primitive Cranberry values
	private static bool IsSimpleClrType(Type t) {
		return t == typeof(string) || t == typeof(bool) ||
		       t == typeof(double) || t == typeof(float) ||
		       t == typeof(int) || t == typeof(long) ||
		       t == typeof(short) || t == typeof(byte) ||
		       t == typeof(uint) || t == typeof(ulong) ||
		       t == typeof(decimal) || t == typeof(char);
	}

	// Try to find op_Implicit/op_Explicit on 'type' that returns a simple CLR type (bool, numeric, string, char)
	private static MethodInfo? FindConversionOperatorReturningSimple(Type type) {
		var candidates = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Where(m => (m.Name == "op_Implicit" || m.Name == "op_Explicit") && IsSimpleClrType(m.ReturnType))
			.ToArray();
		// Prefer bool return, then numeric, then string/char
		if (candidates.Length == 0) return null;
		var pick = candidates.FirstOrDefault(m => m.ReturnType == typeof(bool))
		           ?? candidates.FirstOrDefault(m => m.ReturnType == typeof(double) || m.ReturnType == typeof(float) || m.ReturnType == typeof(int) || m.ReturnType == typeof(long))
		           ?? candidates.FirstOrDefault(m => m.ReturnType == typeof(string))
		           ?? candidates.FirstOrDefault();
		return pick;
	}

	// If a type has exactly one public instance property/field that is a simple CLR type, return its value.
	private static bool TryGetSinglePrimitiveMemberValue(object obj, out object? value) {
		value = null;
		var type = obj.GetType();
		var publicProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.GetMethod != null && IsSimpleClrType(p.PropertyType)).ToArray();
		var publicFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
			.Where(f => IsSimpleClrType(f.FieldType)).ToArray();

		if (publicProps.Length == 1 && publicFields.Length == 0) {
			try {
				value = publicProps[0].GetValue(obj);
				return true;
			} catch {
				return false;
			}
		}

		if (publicFields.Length == 1 && publicProps.Length == 0) {
			try {
				value = publicFields[0].GetValue(obj);
				return true;
			} catch {
				return false;
			}
		}

		return false;
	}
}