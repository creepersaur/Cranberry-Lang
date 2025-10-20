using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CString(string value) : IMemberAccessible {
	public readonly string Value = value;

	public override string ToString() => Value;

	// Pick either implicit OR explicit. Here is explicit:
	public static explicit operator string(CString cstr) {
		if (cstr == null) throw new ArgumentNullException(nameof(cstr));
		return cstr.Value;
	}

	// From string to CString
	public static implicit operator CString(string str) {
		return new CString(str);
	}


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
				"length" => new InternalFunction((_, args) => {
					if (args.Length != 0)
						throw new RuntimeError("`length()` expects 0 arguments.");

					return (double)Value.Length;
				}),

				"chars" => new InternalFunction((_, args) => {
					if (args.Length != 0)
						throw new RuntimeError("`chars()` expects 0 arguments.");

					return new CList(Value.ToCharArray().Cast<object>().ToList());
				}),

				"lower" => new InternalFunction((_, args) => {
					if (args.Length != 0)
						throw new RuntimeError("`lower()` expects 0 arguments.");

					return new CString(Value.ToLower());
				}),

				"upper" => new InternalFunction((_, args) => {
					if (args.Length != 0)
						throw new RuntimeError("`upper()` expects 0 arguments.");

					return new CString(Value.ToUpper());
				}),

				"split" => new InternalFunction((_, args) => {
					if (args.Length > 1)
						throw new RuntimeError("`split(separator)` expects 0-1 arguments.");

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

				"trim" => new InternalFunction((_, args) => {
					if (args.Length != 0) throw new RuntimeError("`trim()` expects 0 arguments.");
					return new CString(Value.Trim());
				}),

				"starts_with" => new InternalFunction((_, args) => {
					if (args.Length != 1) throw new RuntimeError("`starts_with(prefix)` expects 1 argument.");

					string p = args[0] is CString csp ? csp.Value : args[0] as string ?? throw new RuntimeError("`starts_with` expects a string argument.");
					return Value.StartsWith(p);
				}),

				"ends_with" => new InternalFunction((_, args) => {
					if (args.Length != 1) throw new RuntimeError("`ends_with(suffix)` expects 1 argument.");

					string p = args[0] is CString csp ? csp.Value : args[0] as string ?? throw new RuntimeError("`ends_with` expects a string argument.");
					return Value.EndsWith(p);
				}),

				"replace" => new InternalFunction((_, args) => {
					if (args.Length != 2) throw new RuntimeError("`replace(old, new)` expects 2 arguments.");
					string oldv = args[0] is CString cs0 ? cs0.Value : args[0] as string ?? throw new RuntimeError("`replace` expects string arguments.");
					string newv = args[1] is CString cs1 ? cs1.Value : args[1] as string ?? throw new RuntimeError("`replace` expects string arguments.");
					return new CString(Value.Replace(oldv, newv));
				}),

				"contains" => new InternalFunction((_, args) => {
					if (args.Length != 1) throw new RuntimeError("`contains(substr)` expects 1 argument.");
					string s = args[0] switch {
						CString cs => cs.Value,
						string str => str,
						_ => throw new RuntimeError("`contains` expects a string argument.")
					};
					return new NumberNode(null, Value.Contains(s) ? 1 : 0);
				}),

				"find" => new InternalFunction((_, args) => {
					if (args.Length != 1) throw new RuntimeError("`find(substr)` expects 1 argument.");
					string s = args[0] switch {
						CString cs => cs.Value,
						string str => str,
						_ => throw new RuntimeError("`find` expects a string argument.")
					};
					return new NumberNode(null, Value.IndexOf(s, StringComparison.Ordinal));
				}),

				"sub" => new InternalFunction((_, args) => {
					if (args.Length is < 1 or > 2) throw new RuntimeError("`sub(start, length)` expects 1-2 arguments.");
					if (args[0] is not double s) throw new RuntimeError("`sub` start must be a number.");

					var start = Misc.DoubleToIndex(s, Value.Length, true);

					if (args.Length == 1) {
						return new CString(Value[start..]);
					}

					if (args[1] is not double len) throw new RuntimeError("`sub` length must be a number.");
					var length = Convert.ToInt32(len);
					return new CString(Value.Substring(start, length));
				}),
				
				"is_digit" => new InternalFunction((_, args) => {
					if (args.Length != 0)
						throw new RuntimeError("`is_digit()` expects 0 arguments.");
					return Value.All(char.IsDigit);
				}),

				"is_alpha" => new InternalFunction((_, args) => {
					if (args.Length != 0)
						throw new RuntimeError("`is_alpha()` expects 0 arguments.");
					return Value.All(char.IsLetter);
				}),

				"is_letter" => new InternalFunction((_, args) => {
					if (args.Length != 0)
						throw new RuntimeError("`is_letter()` expects 0 arguments.");
					return Value.All(char.IsLetter);
				}),

				"is_alphanum" => new InternalFunction((_, args) => {
					if (args.Length != 0)
						throw new RuntimeError("`is_alphanum()` expects 0 arguments.");
					return Value.All(char.IsLetterOrDigit);
				}),

				"is_number" => new InternalFunction((_, args) => {
					if (args.Length != 0)
						throw new RuntimeError("`is_number()` expects 0 arguments.");
    
					// allow optional leading + or - and at most one decimal point
					if (string.IsNullOrWhiteSpace(Value)) return false;

					bool hasDecimal = false;
					for (int i = 0; i < Value.Length; i++) {
						char c = Value[i];
						if (c == '.' && !hasDecimal) {
							hasDecimal = true;
						} else if ((c == '-' || c == '+') && i == 0) {
							// leading sign is ok
						} else if (!char.IsDigit(c)) {
							return false;
						}
					}
					return true;
				}),

				"is_upper" => new InternalFunction((_, args) => {
					if (args.Length != 0)
						throw new RuntimeError("`is_upper()` expects 0 arguments.");
					return Value.All(char.IsUpper);
				}),

				"is_lower" => new InternalFunction((_, args) => {
					if (args.Length != 0)
						throw new RuntimeError("`is_lower()` expects 0 arguments.");
					return Value.All(char.IsLower);
				}),

				_ => throw new RuntimeError($"Tried getting unknown member `{member}` on type `string`")
			};

		throw new RuntimeError($"Tried getting unknown member `{member}` on type `string`");
	}
}