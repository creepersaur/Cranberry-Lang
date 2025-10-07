using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CString(string value) : IMemberAccessible {
	public readonly string Value = value;

	public override string ToString() => $"{Value}";

	/////////////////////////////////////////////////////////
	// MEMBERSHIP
	///////////////////////////////////////////////////////// 

	public object GetMember(object? member) {
		if (member is double num) {
			int index = Misc.DoubleToIndex(num, Value.Length, true);
			return new CString(Value[index].ToString());
		}

		if (member is CRange range) {
			if (range.Inclusive)
				return new CString(Value[
					Misc.DoubleToIndex(range.Start, Value.Length + 1, true)
						..Misc.DoubleToIndex(range.End, Value.Length + 1, true)
				]);
			
			return new CString(Value[
				Misc.DoubleToIndex(range.Start, Value.Length, true)
					..Misc.DoubleToIndex(range.End, Value.Length, true)
			]);
		}

		if (member is string name)
			return name switch {
				"Length" => new InternalFunction(args => {
					if (args.Length != 0)
						throw new RuntimeError("`Length()` expects 0 arguments.");

					return (double)Value.Length;
				}),

				"Chars" => new InternalFunction(args => {
					if (args.Length != 0)
						throw new RuntimeError("`Chars()` expects 0 arguments.");

					return new CList(Value.ToCharArray().Cast<object>().ToList());
				}),

				"Lower" => new InternalFunction(args => {
					if (args.Length != 0)
						throw new RuntimeError("`Lower()` expects 0 arguments.");

					return new CString(Value.ToLower());
				}),

				"Upper" => new InternalFunction(args => {
					if (args.Length != 0)
						throw new RuntimeError("`Upper()` expects 0 arguments.");

					return new CString(Value.ToUpper());
				}),

				"Split" => new InternalFunction(args => {
					if (args.Length > 1)
						throw new RuntimeError("`Split(separator)` expects 0-1 arguments.");

					if (args.Length > 0)
						if (args[0] is CString sep) {
							if (sep.Value.Length < 1)
								return new CList(Value.ToCharArray().Select(object (x) => x.ToString()).ToList());
								
							return new CList(Value.Split(sep.Value).Select(object (x) => x).ToList());
						} else {
							throw new RuntimeError("`Split(separator)` expects 0-1 string arguments.");
						}
					
					return new CList(Value.Split(" ").Select(object (x) => new CString(x)).ToList());
				}),
				
				"Trim" => new InternalFunction(args => {
					if (args.Length != 0) throw new RuntimeError("`Trim()` expects 0 arguments.");
					return new CString(Value.Trim());
				}),

				"StartsWith" => new InternalFunction(args => {
					if (args.Length != 1) throw new RuntimeError("`StartsWith(prefix)` expects 1 argument.");
					
					string p = args[0] is CString csp ? csp.Value : args[0] as string ?? throw new RuntimeError("`StartsWith` expects a string argument.");
					return Value.StartsWith(p);
				}),

				"EndsWith" => new InternalFunction(args => {
					if (args.Length != 1) throw new RuntimeError("`EndsWith(suffix)` expects 1 argument.");
					
					string p = args[0] is CString csp ? csp.Value : args[0] as string ?? throw new RuntimeError("`EndsWith` expects a string argument.");
					return Value.EndsWith(p);
				}),
				
				"Replace" => new InternalFunction(args => {
					if (args.Length != 2) throw new RuntimeError("`Replace(old, new)` expects 2 arguments.");
					string oldv = args[0] is CString cs0 ? cs0.Value : args[0] as string ?? throw new RuntimeError("`Replace` expects string arguments.");
					string newv = args[1] is CString cs1 ? cs1.Value : args[1] as string ?? throw new RuntimeError("`Replace` expects string arguments.");
					return new CString(Value.Replace(oldv, newv));
				}),

                "Contains" => new InternalFunction(args => {
                    if (args.Length != 1) throw new RuntimeError("`Contains(substr)` expects 1 argument.");
                    string s = args[0] switch {
                        CString cs => cs.Value,
                        string str => str,
                        _ => throw new RuntimeError("`Contains` expects a string argument.")
                    };
                    return new NumberNode(Value.Contains(s) ? 1 : 0);
                }),
				
				"Find" => new InternalFunction(args => {
					if (args.Length != 1) throw new RuntimeError("`IndexOf(substr)` expects 1 argument.");
					string s = args[0] switch {
						CString cs => cs.Value,
						string str => str,
						_ => throw new RuntimeError("`IndexOf` expects a string argument.")
					};
					return new NumberNode(Value.IndexOf(s, StringComparison.Ordinal));
				}),
				
				"Sub" => new InternalFunction(args => {
					if (args.Length is < 1 or > 2) throw new RuntimeError("`Substring(start, length)` expects 1-2 arguments.");
					if (args[0] is not double s) throw new RuntimeError("`Substring` start must be a number.");

					var start = Misc.DoubleToIndex(s, Value.Length, true);

					if (args.Length == 1) {
						return new CString(Value[start..]);
					}

					if (args[1] is not double len) throw new RuntimeError("`Substring` length must be a number.");
					var length = Convert.ToInt32(len);
					return new CString(Value.Substring(start, length));
				}),
				
				_ => throw new RuntimeError($"Tried getting unknown member `{member}` on type `string`")
			};

		throw new RuntimeError($"Tried getting unknown member `{member}` on type `string`");
	}
}