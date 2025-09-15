using Cranberry.Errors;
using Cranberry.Builtin;

namespace Cranberry.Types;

public class CList(List<object> items) : IMemberAccessible {
	public readonly List<object> Items = items;

	public object GetMember(object member) {
		if (member is string name) {
			switch (name) {
				case "length":
					return Items.Count;
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

	// if (member is double md) {
	//        int idx = DoubleToIndex(md, list.Items.Count, allowNegative: true);
	//        return list.Items[idx];
	//    }
}