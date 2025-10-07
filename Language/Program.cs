using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry;

public class Program {
	public readonly Dictionary<string, string> Includes = new();
	public static Interpreter? interpreter;
	public readonly Env original_env;

	public Program(bool is_build) {
		interpreter = new Interpreter(is_build);
		original_env = interpreter.env;
	}

	public void RunFile(string text, string path) {
		var previousEnv = interpreter!.env;

		try {
			interpreter.env = original_env;

			var tokens = new Lexer(text).GetTokens();
			// Lexer.PrintTokens(tokens);
			var ast = new List<Node>(Math.Max(16, tokens.Count / 4));
			var important = new List<Node>(8);

			var parser = new Parser(tokens.ToArray());

			while (parser.PeekAhead() != null) {
				if (parser.Check(";")) {
					parser.Advance();
					continue;
				}

				var node = parser.Parse();
				if (node is FunctionDef or ClassDef) {
					important.Add(node);
				} else {
					ast.Add(node);
				}

				if (parser.Check(";") || parser.Check("\n")) {
					parser.Advance();
				}
			}

			foreach (var node in important) {
				try {
					RunNode(node, path);
				} catch (ReturnException) {
					return;
				} catch (OutException) {}
			}

			foreach (var node in ast) {
				try {
					RunNode(node, path);
				} catch (ReturnException) {
					return;
				} catch (OutException) {}
			}
		} finally {
			interpreter.env = previousEnv;
		}
	}

	public void RunNode(Node node, string path) {
		try {
			interpreter!.Evaluate(node);
		} catch (BreakException) {
			throw new RuntimeError("`break` must only be used in loops.");
		} catch (IncludeFileException include) {
			if (include.Path is List<object> l) {
				foreach (var p in l) {
					if (Includes.TryGetValue((string)p, out var value)) {
						RunFile(value, (string)p);
						return;
					}
					
					var f = new FileInfo((string)p);
					if (f.Exists) {
						if (f.FullName != path) {
							RunFile(File.ReadAllText(f.FullName), f.FullName);
						} else {
							throw new RuntimeError("Cyclic dependency on file self. Cannot `include` file of same path.");
						}
					} else {
						throw new RuntimeError($"Failed to include file: `{f.FullName}`");
					}
				}
			} else {
				if (Includes.TryGetValue((string)include.Path, out var value)) {
					RunFile(value, (string)include.Path);
					return;
				}

				var f = new FileInfo((string)include.Path);
				if (f.Exists) {
					if (f.FullName != path) {
						RunFile(File.ReadAllText(f.FullName), f.FullName);
					} else {
						throw new RuntimeError($"Cyclic dependency on file self. Cannot `include` file of same path.");
					}
				} else {
					throw new RuntimeError($"Failed to include file: `{f.FullName}`");
				}
			}
		}
	}

	public (string, List<string>) CollectFiles(string entry_point) {
		var main = new FileInfo(entry_point);
		var dir = new DirectoryInfo("src");
		var files = new List<string>();
		string entry = main.FullName;

		if (dir.Exists) {
			foreach (var file in dir.GetFiles("*.cb", SearchOption.AllDirectories)) {
				if (file.FullName == main.FullName) continue;

				files.Add(file.FullName);
			}
		}

		return (entry, files);
	}

	public void RunProgram(string entry, List<string> files) {
		foreach (var path in files) {
			RunFile(File.ReadAllText(path), path);
		}

		RunFile(File.ReadAllText(entry), entry);
	}

	public void RunBuild(string entry, Dictionary<string, string> files) {
		foreach (var (name, data) in files) {
			if (name != entry && name != ".srcConfig")
				RunFile(data, name);
		}

		RunFile(files[entry], entry);
	}
}