using Cranberry.Errors;
using Cranberry.Builtin;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CDict : IMemberAccessible {
	public readonly Dictionary<object, object> Items;
	private readonly Dictionary<string, InternalFunction>? Functions;

	public object GetMember(object? member) {
		if (Items.TryGetValue(member!, out var item))
			return item;
		
		if (member is string name) {
			if (Functions!.TryGetValue(name, out var func))
				return func;
		}

		throw new RuntimeError($"Tried to get unknown member or value: `{member}` on `dict`. (Maybe try using `.GetOrElse(key, value)`)");
	}

	public void SetMember(object? member, object? value) {
		if (member is CString c) member = c.Value;
		if (value is CString v) value = v.Value;
		
		Items[member!] = value!;
	}

	public CDict(Dictionary<object, object> items) {
		Items = items ?? throw new ArgumentNullException(nameof(items));

		Functions = FuncGen.GenerateFunctions([
			FuncGen.FuncInternal(
				"length",
				(_, args) => {
					if (args.Length > 0) throw new RuntimeError("`length()` expects 0 arguments.");
					return Items.Count;
				}
			),

			FuncGen.FuncInternal(
				"keys",
				(_, args) => {
					if (args.Length != 0) throw new RuntimeError("`keys()` expects 0 arguments.");
					return new CList(Items.Keys.ToList());
				}
			),

			FuncGen.FuncInternal(
				"values",
				(_, args) => {
					if (args.Length != 0) throw new RuntimeError("`values()` expects 0 arguments.");
					return new CList(Items.Values.ToList());
				}
			),

			FuncGen.FuncInternal(
				"set",
				(_, args) => {
					if (args.Length != 2) throw new RuntimeError("`set(key, value)` expects 2 arguments.");

					var member = args[0];
					var value = args[1];
					
					if (member is CString c) member = c.Value;
					if (value is CString d) value = d.Value;
					
					Items[member!] = value!;
					
					return new NullNode();
				}
			),

			FuncGen.FuncInternal(
				"get",
				(_, args) => {
					if (args.Length != 1) throw new RuntimeError("`get(key)` expects 1 argument.");
					
					if (Items.TryGetValue(args[0]!, out var value))
						return value;
					
					if (args[0] is CString c && Items.TryGetValue(c.Value, out var v))
						return v;
					
					throw new RuntimeError("Tried getting a value that isn't in the dict. (Maybe try `GetOrElse(key, value)`)");
				}
			),

			FuncGen.FuncInternal(
				"get_or_else",
				(_, args) => {
					if (args.Length != 2) throw new RuntimeError("`get_or_else(key, value)` expects 2 arguments.");
					if (Items.TryGetValue(args[0]!, out var value))
						return value;
					
					if (args[0] is CString c && Items.TryGetValue(c.Value, out var v))
						return v;

					return args[1];
				}
			),

			FuncGen.FuncInternal(
				"remove",
				(_, args) => {
					if (args.Length != 1) throw new RuntimeError("`remove(key)` expects 1 argument.");
					Items.Remove(args[0]!);
					return new NullNode();
				}
			),

			FuncGen.FuncInternal(
				"clear",
				(_, args) => {
					if (args.Length != 0) throw new RuntimeError("`clear()` expects 0 arguments.");
					Items.Clear();
					return new NullNode();
				}
			),

			FuncGen.FuncInternal(
				"merge",
				(_, args) => {
					if (args.Length != 1) throw new RuntimeError("`merge(dict)` expects 1 argument.");
					if (args[0] is CDict c) {
						foreach (var (key, value) in c.Items) {
							Items[key] = value;
						}
					} else {
						throw new RuntimeError("`merge(dict) takes in a `dict` and adds its values.");
					}

					return new NullNode();
				}
			),

			FuncGen.FuncInternal(
				"clone",
				(_, args) => {
					if (args.Length != 0) throw new RuntimeError("`clone()` expects 0 arguments.");
					return new CDict(Items.Copy());
				}
			),

			FuncGen.FuncInternal(
				"has",
				(_, args) => {
					if (args.Length != 1) throw new RuntimeError("`has(obj)` expects 1 argument.");
					if (args[0] is CString c)
						return Items.ContainsValue(c.Value) || Items.ContainsValue(args[0]!);
					
					return Items.ContainsValue(args[0]!);
				}
			),

			FuncGen.FuncInternal(
				"has_key",
				(_, args) => {
					if (args.Length != 1) throw new RuntimeError("`has_key(obj)` expects 1 argument.");
					
					if (args[0] is CString c)
						return Items.ContainsKey(c.Value) || Items.ContainsKey(args[0]!);
					
					return Items.ContainsKey(args[0]!);
				}
			),
		]);
	}
}