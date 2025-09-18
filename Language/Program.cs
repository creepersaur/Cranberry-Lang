using Cranberry.Errors;

namespace Cranberry;

public class Program {
	public readonly Env original_env;
	public readonly Interpreter interpreter = new();

	public Program() {
		original_env = interpreter.env;
	}
	
	public void RunFile(string text) {
		interpreter.env = original_env;
		
		var tokens = new Lexer(text).GetTokens();
		var parser = new Parser(tokens.ToArray());
		
		while (parser.PeekAhead() != null) {
			try {
				interpreter.Evaluate(parser.Parse());
			} catch (ReturnException) {
				throw new RuntimeError("Cannot `return` in main scope.");
			} catch (OutException) {
				throw new RuntimeError("Cannot `out` in main scope.");
			} catch (BreakException) {
				throw new RuntimeError("`break` must only be used in loops.");
			} catch (IncludeFileException include) {
				if (include.Path is List<object> l) {
					foreach (var p in l) {
						var f = new FileInfo((string)p);
						if (f.Exists) {
							RunFile(File.ReadAllText(f.FullName));
						} else {
							throw new RuntimeError($"Failed to include file: `{f.FullName}`");
						}
					}
				} else {
					var f = new FileInfo((string)include.Path);
					if (f.Exists) {
						RunFile(File.ReadAllText(f.FullName));
					} else {
						throw new RuntimeError($"Failed to include file: `{f.FullName}`");
					}
				}
			}

			if (parser.Check(";")) {
				parser.Advance();
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
			RunFile(File.ReadAllText(path));
		}
		RunFile(File.ReadAllText(entry));
	}
}