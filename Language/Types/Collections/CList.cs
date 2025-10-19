using Cranberry.Errors;
using Cranberry.Builtin;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CList : IMemberAccessible {
	public readonly List<object> Items;
	public readonly bool IsTuple;
	private readonly Dictionary<string, InternalFunction>? Functions;

	public override string ToString() => Misc.FormatValue(this)!;

	public object GetMember(object? member) {
		switch (member) {
			case string name when Functions!.TryGetValue(name, out var value):
				return value;

			case double d: {
				int index = Misc.DoubleToIndex(d, Items.Count, true);
				if (index >= Items.Count)
					throw new RuntimeError($"Tried to get item at index ({index}) but length of list is ({Items.Count})");

				if (Items[index] is string s)
					return new CString(s);

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

			if (value is CString c) value = c.Value;

			Items[index] = value!;
			return;
		}

		throw new RuntimeError($"Cannot set member `{member}` on list.");
	}

	public CList(List<object> items, bool is_tuple = false) {
		Items = (items ?? throw new ArgumentNullException(nameof(items)));
		IsTuple = is_tuple;

		Functions = FuncGen.GenerateFunctions([
			FuncGen.FuncInternal(
				"length",
				args => {
					if (args.Length > 0) throw new RuntimeError("`length()` expects 0 arguments.");
					return Items.Count;
				}
			),

			IsTuple ? null : FuncGen.FuncInternal("push", args => {
				if (args.Length != 1) throw new RuntimeError("`push(item)` expects 1 argument.");
				if (args[0] is CString c) Items.Add(c.Value);
				else Items.Add(args[0]!);
				return new NullNode();
			}),

			IsTuple ? null : FuncGen.FuncInternal(
				"pop",
				args => {
					if (args.Length > 0) throw new RuntimeError("`pop()` expects 0 arguments.");
					if (Items.Count < 1) throw new RuntimeError("`List` needs at least 1 item to `pop()`.");

					var item = Items.Last();
					Items.RemoveAt(Items.Count - 1);

					return item;
				}
			),

			IsTuple ? null : FuncGen.FuncInternal(
				"remove",
				args => {
					if (args.Length != 1) throw new RuntimeError("`remove(index)` expects 1 argument.");
					int index = Misc.DoubleToIndex(args[0]!, Items.Count, true);
					Items.RemoveAt(index);
					return new NullNode();
				}
			),

			IsTuple ? null : FuncGen.FuncInternal(
				"insert",
				args => {
					if (args.Length != 2) throw new RuntimeError("`insert(index, item)` expects 2 arguments.");
					int index = Misc.DoubleToIndex(args[0]!, Items.Count, true);
					Items.Insert(index, args[1]!);
					return new NullNode();
				}
			),

			FuncGen.FuncInternal(
				"find",
				args => {
					if (args.Length != 1) throw new RuntimeError("`find(item)` expects 1 argument.");

					// if CString, check underlying string value as well
					if (args[0] is CString c)
						return Items.Contains(c.Value) ? Items.IndexOf(c.Value) : new NullNode();

					return Items.Contains(args[0]!) ? Items.IndexOf(args[0]!) : new NullNode();
				}
			),

			FuncGen.FuncInternal(
				"last",
				args => {
					if (args.Length != 0) throw new RuntimeError("`last()` expects 0 arguments.");
					return Items.Count > 0 ? Items.Last() : new NullNode();
				}
			),

			FuncGen.FuncInternal(
				"first",
				args => {
					if (args.Length != 0) throw new RuntimeError("`first()` expects 0 arguments.");
					return Items.Count > 0 ? Items.First() : new NullNode();
				}
			),

			IsTuple ? null : FuncGen.FuncInternal(
				"clear",
				args => {
					if (args.Length != 0) throw new RuntimeError("`clear()` expects 0 arguments.");
					Items.Clear();
					return new NullNode();
				}
			),

			IsTuple ? null : FuncGen.FuncInternal(
				"append",
				args => {
					if (args.Length != 1) throw new RuntimeError("`append(list)` expects 1 argument.");

					if (args[0] is CList c) {
						foreach (var i in c.Items) {
							Items.Add(i);
						}
					} else {
						throw new RuntimeError("`append(list)` takes in a list and adds its values to the end.");
					}

					return new NullNode();
				}
			),

			FuncGen.FuncInternal(
				"clone",
				args => {
					if (args.Length != 0) throw new RuntimeError("`clone()` expects 0 arguments.");
					// preserve tuple-ness: clone of a tuple is still a tuple (fixed-size)
					return new CList(Items.Copy(), IsTuple);
				}
			),

			FuncGen.FuncInternal(
				"has",
				args => {
					if (args.Length != 1) throw new RuntimeError("`has(obj)` expects 1 argument.");
					if (args[0] is CString c)
						return Items.Contains(c.Value) || Items.Contains(args[0]!);

					return Items.Contains(args[0]!);
				}
			),

			// convenience: convert to a mutable list copy
			FuncGen.FuncInternal(
				"to_list",
				args => {
					if (args.Length != 0) throw new RuntimeError("`to_list()` expects 0 arguments.");
					return new CList(Items.Copy());
				}
			)
		]);

		Functions["map"] = new InternalFunction(args => {
			if (args.Length != 1) throw new RuntimeError("`map( func(value) )` expects 1 argument.");
			if (args[0] is not FunctionNode f)
				throw new RuntimeError("`map( func(value) )` expects 1 function argument.");

			var list = new List<object>();
			foreach (var v in Items) {
				list.Add(Program.interpreter!.Evaluate(new FunctionCall("", [v]) {
					Target = (Node)Program.interpreter.Evaluate(f)
				}));
			}

			// preserve tuple-ness: mapping a tuple returns a tuple; mapping a list returns a list
			return new CList(list, IsTuple);
		});
	}
}