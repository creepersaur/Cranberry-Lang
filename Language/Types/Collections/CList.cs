using Cranberry.Errors;
using Cranberry.Builtin;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CList : IMemberAccessible {
	public List<object> Items;
	private readonly Dictionary<string, InternalFunction>? Functions;

	public object GetMember(object? member) {
		switch (member) {
			case string name when Functions!.TryGetValue(name, out var value):
				return value;
			
			case double d: {
				int index = Misc.DoubleToIndex(d, Items.Count, true);
				if (index >= Items.Count)
					throw new RuntimeError($"Tried to get item at index ({index}) but length of list is ({Items.Count})");
			
				return Items[index];
			}
			
			default:
				throw new RuntimeError($"Tried to get unknown member: `{member}` on `list`.");
		}
	}

	public void SetMember(object? member, object? value) {
		if (member is double d) {
			int index = Misc.DoubleToIndex(d, Items.Count, true);
			if (index >= Items.Count)
				throw new RuntimeError($"Tried to get item at index ({index}) but length of list is ({Items.Count})");

			Items[index] = value!;
			return;
		}
		
		throw new RuntimeError($"Cannot set member `{member}` on list.");
	}
	
	public CList(List<object> items) {
		Items = (items ?? throw new ArgumentNullException(nameof(items)));
		
		Functions = FuncGen.GenerateFunctions([
			FuncGen.FuncInternal(
				"Length", 
				args => {
					if (args.Length > 0) throw new RuntimeError("`Length()` expects 0 arguments.");
					return Items.Count;
				}
			),
			
			FuncGen.FuncInternal(
				"Push", 
				args => {
					if (args.Length != 1) throw new RuntimeError("`Push(item)` expects 1 argument.");
					
					if (args[0] is CString c)
						Items.Add(new CString(c.Value));
					else
						Items.Add(args[0]!);
					
					return new NullNode();
				}
			),
			
			FuncGen.FuncInternal(
				"Pop", 
				args => {
					if (args.Length > 0) throw new RuntimeError("`Pop()` expects 0 arguments.");
					if (Items.Count < 1) throw new RuntimeError("`List` needs at least 1 item to `Pop()`.");

					var item = Items.Last();
					Items.RemoveAt(Items.Count - 1);

					return item;
				}
			),
			
			FuncGen.FuncInternal(
				"Remove", 
				args => {
					if (args.Length != 1) throw new RuntimeError("`Remove(index)` expects 1 argument.");
					int index = Misc.DoubleToIndex(args[0]!, Items.Count, true);
					Items.RemoveAt(index);
					return new NullNode();
				}
			),
			
			FuncGen.FuncInternal(
				"Insert", 
				args => {
					if (args.Length != 2) throw new RuntimeError("`Remove(index, item)` expects 2 arguments.");
					int index = Misc.DoubleToIndex(args[0]!, Items.Count, true);
					Items.Insert(index, args[1]!);
					return new NullNode();
				}
			),

			FuncGen.FuncInternal(
				"Find", 
				args => {
					if (args.Length != 1) throw new RuntimeError("`Remove(item)` expects 1 arguments.");

					if (Items.Contains(args))
						return Items.IndexOf(args[0]!);
					
					return new NullNode();
				}
			),

			FuncGen.FuncInternal(
				"Last", 
				args => {
					if (args.Length != 0) throw new RuntimeError("`Last()` expects 0 arguments.");
					
					return Items.Count > 0 ? Items.Last() : new NullNode();
				}
			),
			
			FuncGen.FuncInternal(
				"First", 
				args => {
					if (args.Length != 0) throw new RuntimeError("`Last()` expects 0 arguments.");
					
					return Items.Count > 0 ? Items.First() : new NullNode();
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
				"Append", 
				args => {
					if (args.Length != 1) throw new RuntimeError("`Append(list)` expects 1 argument.");
					if (args[0] is CList c) {
						foreach (var i in c.Items) {
							Items.Add(i);
						}
					} else {
						throw new RuntimeError("`Append(list) takes in a list and adds its values to the end.");
					}
					return new NullNode();
				}
			),
			
			FuncGen.FuncInternal(
				"Clone", 
				args => {
					if (args.Length != 0) throw new RuntimeError("`Clone()` expects 0 arguments.");
					return new CList(Items.Copy());
				}
			),
			
			FuncGen.FuncInternal(
				"Has",
				args => {
					if (args.Length != 1) throw new RuntimeError("`Has(obj)` expects 1 argument.");
					if (args[0] is CString c)
						return Items.Contains(c.Value) || Items.Contains(args[0]!);
					
					return Items.Contains(args[0]!);
				}
			),
		]);
		
		Functions["Map"] = new InternalFunction(args => {
			if (args.Length != 1) throw new RuntimeError("`Map( func(value) )` expects 1 argument.");
			if (args[0] is not FunctionNode f)
				throw new RuntimeError("`Map( func(value) )` expects 1 function argument.");
					
			var list = new List<object>();
			foreach (var v in Items) {
				list.Add(Program.interpreter!.Evaluate(new FunctionCall("", [v]) {
					Target = (Node)Program.interpreter.Evaluate(f)
				}));
			}
					
			return new CList(list);
		});
	}
}