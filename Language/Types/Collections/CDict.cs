using Cranberry.Errors;
using Cranberry.Builtin;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CDict : IMemberAccessible {
	public readonly Dictionary<object, object> Items;
	private static Dictionary<string, InternalFunction>? Functions;

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
		if (member is CString c)
			Items[c.Value] = value!;
		else {
			Items[member!] = value!;
		}
	}

	public CDict(Dictionary<object, object> items) {
		Items = items ?? throw new ArgumentNullException(nameof(items));

		Functions = FuncGen.GenerateFunctions([
			FuncGen.FuncInternal(
				"Length",
				args => {
					if (args.Length > 0) throw new RuntimeError("`Length()` expects 0 arguments.");
					return Items.Count;
				}
			),

			FuncGen.FuncInternal(
				"Keys",
				args => {
					if (args.Length != 1) throw new RuntimeError("`Keys()` expects 0 arguments.");
					return new CList(Items.Keys.ToList());
				}
			),

			FuncGen.FuncInternal(
				"Values",
				args => {
					if (args.Length != 1) throw new RuntimeError("`Values()` expects 0 arguments.");
					return new CList(Items.Values.ToList());
				}
			),

			FuncGen.FuncInternal(
				"Set",
				args => {
					if (args.Length != 2) throw new RuntimeError("`Set(key, value)` expects 2 arguments.");
					Items[args[0]!] = args[1]!;
					return new NullNode();
				}
			),

			FuncGen.FuncInternal(
				"Get",
				args => {
					if (args.Length != 1) throw new RuntimeError("`Get(key)` expects 1 argument.");
					if (Items.TryGetValue(args[0]!, out var value))
						return value;
					throw new RuntimeError("Tried getting a value that isn't in the dict.");
				}
			),

			FuncGen.FuncInternal(
				"GetOrElse",
				args => {
					if (args.Length != 2) throw new RuntimeError("`GetOrElse(key, value)` expects 2 arguments.");
					return Items.TryGetValue(args[0]!, out var value) ? value : args[1]!;
				}
			),

			FuncGen.FuncInternal(
				"Remove",
				args => {
					if (args.Length != 1) throw new RuntimeError("`Remove(key)` expects 1 argument.");
					Items.Remove(args[0]!);
					return new NullNode();
				}
			),

			FuncGen.FuncInternal(
				"Clear",
				args => {
					if (args.Length != 0) throw new RuntimeError("`Clear()` expects 0 arguments.");
					Items.Clear();
					return new NullNode();
				}
			),

			FuncGen.FuncInternal(
				"Merge",
				args => {
					if (args.Length != 1) throw new RuntimeError("`Merge(dict)` expects 1 argument.");
					if (args[0] is CDict c) {
						foreach (var (key, value) in c.Items) {
							Items[key] = value;
						}
					} else {
						throw new RuntimeError("`Merge(dict) takes in a `dict` and adds its values.");
					}

					return new NullNode();
				}
			),

			FuncGen.FuncInternal(
				"Clone",
				args => {
					if (args.Length != 0) throw new RuntimeError("`Clone()` expects 0 arguments.");
					return new CDict(Items.Copy());
				}
			),

			FuncGen.FuncInternal(
				"Has",
				args => {
					if (args.Length != 1) throw new RuntimeError("`Has(obj)` expects 1 argument.");
					return Items.ContainsValue(args[0]!);
				}
			),

			FuncGen.FuncInternal(
				"HasKey",
				args => {
					if (args.Length != 1) throw new RuntimeError("`HasKey(obj)` expects 1 argument.");
					Console.WriteLine("Something: {0} | {1}", args[0], args[0]!.GetType());

					if (args[0] is CString c)
						return Items.ContainsKey(c.Value) || Items.ContainsKey(args[0]!);
					
					return Items.ContainsKey(args[0]!);
				}
			),
		]);
	}
}