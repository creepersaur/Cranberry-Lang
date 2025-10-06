using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;
using Cranberry.Types;

namespace Cranberry.Namespaces;

public class N_Http : CNamespace {
	public N_Http(Interpreter interpreter) : base("Http", true) {
		env.Variables.Push(new Dictionary<string, object> {
			["Get"] = new InternalFunction(args => {
				if (args.Length < 1 || args.Length > 2)
					throw new RuntimeError("Get(url, headers?) expects 1 or 2 arguments.");

				var url = args[0] switch {
					CString c => c.Value,
					string s => s,
					_ => throw new RuntimeError("Get(url): url must be a string.")
				};

				var client = new HttpClient();
				client.DefaultRequestHeaders.Add("User-Agent", "CranberryApp");

				// Optional headers
				if (args.Length == 2) {
					if (args[1] is CDict headersDict) {
						foreach (var kv in headersDict.Items) {
							string key = kv.Key.ToString()!;
							string value = kv.Value.ToString()!;
							if (!client.DefaultRequestHeaders.Contains(key))
								client.DefaultRequestHeaders.Add(key, value);
						}
					} else {
						throw new RuntimeError("Get(url, headers): headers must be a dictionary.");
					}
				}

				var response = client.GetAsync(url).GetAwaiter().GetResult();
				string data = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				return new CString(data);
			}),

			["Post"] = new InternalFunction(args => {
				if (args.Length < 2 || args.Length > 3)
					throw new RuntimeError("Post(url, body, headers?) expects 2 or 3 arguments.");

				var url = args[0] switch {
					CString c => c.Value,
					string s => s,
					_ => throw new RuntimeError("Post(url, body): url must be a string.")
				};

				string payload = args[1] switch {
					CString c => c.Value,
					string s => s,
					CList o => JsonSerializer.Serialize(o.Items),
					CDict o => JsonSerializer.Serialize(o.Items),
					{ } o => JsonSerializer.Serialize(o),
					_ => throw new RuntimeError("Post(url, body): body cannot be nil.")
				};

				var client = new HttpClient();
				client.DefaultRequestHeaders.Add("User-Agent", "CranberryApp");

				// Optional headers
				if (args.Length == 3) {
					if (args[2] is CDict headersDict) {
						foreach (var kv in headersDict.Items) {
							string key = kv.Key.ToString()!;
							string value = kv.Value.ToString()!;
							if (!client.DefaultRequestHeaders.Contains(key))
								client.DefaultRequestHeaders.Add(key, value);
						}
					} else {
						throw new RuntimeError("Post(url, body, headers): headers must be a dictionary.");
					}
				}

				using var content = new StringContent(payload, Encoding.UTF8, "application/json");
				var response = client.PostAsync(url, content).GetAwaiter().GetResult();
				string data = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				return new CString(data);
			}),


			["Listen"] = new InternalFunction(args => {
				if (args.Length != 2)
					throw new RuntimeError("Listen(port, callback) expects 2 arguments: port (number) and callback (function).");

				int port = args[0] switch {
					double d => (int)d,
					int i => i,
					_ => throw new RuntimeError("Listen: port must be a number.")
				};

				var listener = new HttpListener();
				listener.Prefixes.Add($"http://localhost:{port}/");
				listener.Start();
				Console.WriteLine($"[Cranberry] Listening on port {port}...");

				if (args[1] is not InternalFunction && args[1] is not FunctionNode)
					throw new RuntimeError($"Listen: callback must be a function. Got `{args[1]}`");

				while (true) {
					try {
						var context = listener.GetContext();
						var request = context.Request;
						var response = context.Response;

						var reqDict = new Dictionary<string, object> {
							["method"] = request.HttpMethod,
							["path"] = request.Url?.AbsolutePath ?? "",
							["query"] = request.Url?.Query ?? "",
							["headers"] = request.Headers,
							["body"] = new CString(new StreamReader(request.InputStream, request.ContentEncoding).ReadToEnd())
						};
						var req = new CDict(reqDict.Select(pair => new KeyValuePair<object, object>(pair.Key, pair.Value)).ToDictionary());

						var resultObj = args[1] switch {
							InternalFunction i => i.Call(req),
							FunctionNode i => interpreter.Evaluate(new FunctionCall("", [req]) {
								Target = i
							}),
							_ => null
						};

						// Default response values
						int status = 200;
						Dictionary<string, string> headers = new();
						string body = "OK";

						// If callback returned a dictionary, use it
						if (resultObj is CDict dict) {
							if (dict.Items.TryGetValue("status", out var s)) status = Convert.ToInt32(s);
							if (dict.Items.TryGetValue("body", out var b)) body = b.ToString() ?? "OK";
							if (dict.Items.TryGetValue("headers", out var h) && h is CDict hdrs) {
								foreach (var kv in hdrs.Items) {
									headers[kv.Key.ToString()!] = kv.Value.ToString()!;
								}
							}
						} else if (resultObj is CString c) {
							body = c.Value;
						} else if (resultObj is string str) {
							body = str;
						} else if (resultObj is double d) {
							body = d.ToString(CultureInfo.InvariantCulture);
						} else if (resultObj is CList list) {
							body = JsonSerializer.Serialize(list.Items);
						}
						
						// Apply headers
						foreach (var kv in headers)
							response.Headers[kv.Key] = kv.Value;

						response.StatusCode = status;
						byte[] buffer = Encoding.UTF8.GetBytes(body);
						response.ContentLength64 = buffer.Length;
						response.OutputStream.Write(buffer, 0, buffer.Length);
						response.OutputStream.Close();
					} catch (Exception ex) {
						Console.WriteLine($"[Cranberry Listen] Error: {ex}");
					}
				}
			}),
		});
	}
}