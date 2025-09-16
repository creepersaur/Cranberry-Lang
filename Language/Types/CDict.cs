using Cranberry.Errors;
using Cranberry.Builtin;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CDict : IMemberAccessible {
	public readonly Dictionary<object, object> Items;
	private static Dictionary<string, InternalFunction>? Functions;

	public object GetMember(object member) {
		if (member is string name) {
			switch (name) {
				case "length": return Items.Count;
				case "keys": return new CList(Items.Keys.ToList());
				case "values": return new CList(Items.Values.ToList());
				
				// FUNCTIONS
				
				default:
					if (Functions!.TryGetValue(name, out var value))
						return value;
					break;
			}
		}

		if (member is double d) {
			int index = Misc.DoubleToIndex(d, Items.Count, true);
			if (index >= Items.Count)
				throw new RuntimeError($"Tried to get item at index ({index}) but length of List is ({Items.Count})");
			
			return Items[index];
		}
		
		throw new RuntimeError($"Tried to get unknown member: `{member}` on `List`.");
	}
	
	
	public CDict(Dictionary<object, object> items) {
		Items = items ?? throw new ArgumentNullException(nameof(items));
		
		Functions = FuncGen.GenerateFunctions([
			FuncGen.FuncInternal(
				"Set", 
				args => {
					if (args.Length != 2) throw new RuntimeError("`Set(key, value)` expects 2 arguments.");
					Items.Add(args[0]!, args[1]!);
					return new NullNode();
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
					if (args.Length != 1) throw new RuntimeError("`Append(Dict)` expects 1 argument.");
					if (args[0] is CDict c) {
						foreach (var (key, value) in c.Items) {
							Items.Add(key, value);
						}
					} else {
						throw new RuntimeError("`Append(Dict) takes in a `Dict` and adds its values.");
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
		]);
	}
}