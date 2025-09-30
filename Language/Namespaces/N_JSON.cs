using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Types;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cranberry.Namespaces;

public class N_JSON : CNamespace {
	public N_JSON() : base("JSON", true) {
		env.Variables.Push(new Dictionary<string, object> {
			["Stringify"] = new InternalFunction(args => {
				if (args.Length != 1)
					throw new RuntimeError("JSON.Stringify(value) expects 1 argument.");

				try {
					return JsonSerializer.Serialize(args[0]! switch {
						CList v => v.Items,
						CDict v => v.Items,
						_ => args[0]!
					});
				} catch {
					throw new RuntimeError($"Could not serialize `{args[0]}` to JSON string.");
				}
			}),

			["Parse"] = new InternalFunction(args => {
				if (args.Length != 1)
					throw new RuntimeError("JSON.Parse(string) expects 1 argument.");

				string json = (string)args[0]!;

				try {
					using var doc = JsonDocument.Parse(json);
					JsonElement root = doc.RootElement;

					object Convert(JsonElement el) {
						switch (el.ValueKind) {
							case JsonValueKind.Object: {
								var dict = new Dictionary<object, object>();
								foreach (var prop in el.EnumerateObject()) {
									dict[prop.Name] = Convert(prop.Value);
								}
								return new CDict(dict);
							}
							case JsonValueKind.Array: {
								var list = new List<object>();
								foreach (var item in el.EnumerateArray()) {
									list.Add(Convert(item));
								}
								return new CList(list);
							}
							case JsonValueKind.String:
								return el.GetString()!;
							case JsonValueKind.Number:
								// Try to preserve integer if possible, else use double
								if (el.TryGetInt64(out long l)) return l;
								if (el.TryGetDouble(out double d)) return d;
								// fallback to raw string
								return el.GetRawText();
							case JsonValueKind.True:
								return true;
							case JsonValueKind.False:
								return false;
							case JsonValueKind.Null:
								return null!;
							default:
								// Unknown / other kinds (Undefined)
								return el.GetRawText();
						}
					}

					var converted = Convert(root);

					// If top-level is object or array we already wrapped it; otherwise return primitive as-is
					return converted;
				} catch (JsonException je) {
					throw new RuntimeError($"Could not deserialize JSON: {je.Message}");
				} catch (Exception ex) {
					throw new RuntimeError($"JSON.Parse failed: {ex.Message}");
				}
			})
		});
	}
}