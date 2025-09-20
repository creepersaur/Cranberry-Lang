using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry;

public class Program {
	public readonly Env original_env;
	public readonly Interpreter interpreter = new();

	public Program() {
		original_env = interpreter.env;
	}

	public void RunFile(string text, string path) {
		var previousEnv = interpreter.env;

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

				if (parser.Check(";")) {
					parser.Advance();
				}
			}

			foreach (var node in important) {
				RunNode(node, path);
			}
			
			foreach (var node in ast) {
				RunNode(node, path);
			}
		}
		finally
		{
			interpreter.env = previousEnv;
		}
	}

	public void RunNode(Node node, string path) {
		try {
			interpreter.Evaluate(node);
		} catch (ReturnException) {
			throw new RuntimeError("Cannot `return` in main scope.");
		} catch (OutException) {
			throw new RuntimeError("Cannot `out` in main scope.");
		} catch (BreakException) {
			throw new RuntimeError("`break` must only be used in loops.");
		} catch (IncludeFileException include) {
			if (include.Path is List<object> l) {
				foreach (var f in l.Select(p => new FileInfo((string)p))) {
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
			} else {
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

	public (string, List<string>) CollectFiles() {
		var dir = new DirectoryInfo("src");
		var files = new List<string>();
		string? entry = null;

		if (dir.Exists) {
			foreach (var file in dir.GetFiles("*.cb", SearchOption.AllDirectories)) {
				if (Path.GetFileNameWithoutExtension(file.Name) == "main") {
					entry = file.FullName;
					continue;
				}

				files.Add(file.FullName);
			}
		} else {
			var main = new FileInfo("main.cb");
			if (main.Exists)
				return (main.FullName, []);
		}

		return (entry!, files);
	}

	public void RunProgram(string entry, List<string> files) {
		foreach (var path in files) {
			RunFile(File.ReadAllText(path), path);
		}

		RunFile(File.ReadAllText(entry), entry);
	}
}