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

	public int? RunFile(string text, FileInfo path) {
		interpreter!.FileName = path.Name;
		interpreter.FilePath = path.FullName;
		var previousEnv = interpreter.env;

		try {
			interpreter.env = original_env;

			var tokens = new Lexer(text, path.Name, path.FullName).GetTokens();
			// Lexer.PrintTokens(tokens);
			var parser = new Parser(tokens.ToArray(), path);

			var ast = new List<Node>(Math.Max(16, tokens.Count / 4));
			var important = new List<Node>(8);

			try {
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
			} catch (ParseError e) {
				ErrorPrinter.PrintError(e, $"ParseError at line {e.Token!.Line}:{e.Token!.Col}, file `{e.Token.FileName}`");
				return ErrorPrinter.PrintErrorLine(e.Token, ConsoleColor.Magenta, e.FullLine);
			}

			foreach (var node in ast) {
				if (node is BinaryOpNode b) {
					var e = new RuntimeError($"Unexpected binary operation (`{b.Op}`).", b.StartToken);
					ErrorPrinter.PrintError(e, $"ParseError at line {e.StartToken!.Line}:{e.StartToken!.Col + 1}, file `{e.StartToken.FileName}`");
					return ErrorPrinter.PrintErrorLine(e.StartToken, ConsoleColor.Magenta, e.FullLine);
				}
			}
			
			foreach (var node in important) {
				try {
					RunNode(node, path);
				} catch (ReturnException) {
					return null;
				} catch (ExecutionError e) {
					return ErrorPrinter.PrintError(e, $"ExecutionError at line {e.StartToken.Line}:{e.StartToken.Col + 1}, file `{e.StartToken.FileName}`");
				} catch (AssertionError e) {
					return ErrorPrinter.PrintError(e, $"AssertionError at line {e.StartToken.Line}:{e.StartToken.Col + 1}, file `{e.StartToken.FileName}`");
				} catch (OutException) {
				} catch (RuntimeError e) {
					if (e.StartToken != null) {
						ErrorPrinter.PrintError(e, $"RuntimeError at line {e.StartToken.Line}:{e.StartToken.Col + 1}, file `{e.StartToken.FileName}`");
						return ErrorPrinter.PrintErrorLine(e.StartToken, null, e.FullLine);
					}
					return ErrorPrinter.PrintError(e, $"RuntimeError");
				}
			}

			foreach (var node in ast) {
				try {
					RunNode(node, path);
				} catch (ReturnException) {
					return null;
				} catch (ExecutionError e) {
					ErrorPrinter.PrintError(e, "ExecutionError");
					return ErrorPrinter.PrintErrorLine(e.StartToken, null, false);
				} catch (AssertionError e) {
					ErrorPrinter.PrintError(e, "AssertionError");
					return ErrorPrinter.PrintErrorLine(e.StartToken, null, false);
				} catch (OutException) {
				} catch (RuntimeError e) {
					if (e.StartToken != null) {
						ErrorPrinter.PrintError(e, "RuntimeError");
						return ErrorPrinter.PrintErrorLine(e.StartToken, null, e.FullLine);
					}
					return ErrorPrinter.PrintError(e, "RuntimeError");
				} catch (ParseError e) {
					ErrorPrinter.PrintError(e, "ParseError");
					return ErrorPrinter.PrintErrorLine(e.Token!, ConsoleColor.Magenta, e.FullLine);
				}
			}
		} finally {
			interpreter.env = previousEnv;
		}
		
		return null;
	}

	public object? RunNode(Node node, FileInfo path) {
		try {
			return interpreter!.Evaluate(node);
		} catch (BreakException e) {
			throw new RuntimeError("`break` must only be used in loops.", e.StartToken ?? node.StartToken);
		} catch (ContinueException e) {
			throw new RuntimeError("`continue` must only be used in loops.", e.StartToken ?? node.StartToken);
		} catch (IncludeFileException include) {
			IEnumerable<string> paths;

			// Handle single path or list of paths
			if (include.Path is List<object> l) paths = l.Cast<string>();
			else paths = new List<string> { (string)include.Path };

			foreach (var p in paths) {
				var f = new FileInfo(p);
				
				if (Includes.TryGetValue(p, out var value)) {
					RunFile(value, f);
					continue;
				}

				if (f.Exists && !f.Attributes.HasFlag(FileAttributes.Directory)) {
					// It's a file
					if (f.FullName != path.FullName) RunFile(File.ReadAllText(f.FullName), f);
					else throw new RuntimeError("Cyclic dependency on file self. Cannot `include` file of same path.");
				} else if (Directory.Exists(p)) {
					// It's a directory, include all *.cb files
					var dir = new DirectoryInfo(p);
					foreach (var file in dir.GetFiles("*.cb", include.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)) {
						if (file.FullName != path.FullName) RunFile(File.ReadAllText(file.FullName), file);
						else throw new RuntimeError("Cyclic dependency on file self. Cannot `include` file of same path.");
					}
				} else throw new RuntimeError($"Failed to include file or directory: `{p}`");
			}
		}

		return null;
	}

	public (string, List<FileInfo>) CollectFiles(string entry_point) {
		var main = new FileInfo(entry_point);
		var dir = new DirectoryInfo("src");
		var files = new List<FileInfo>();
		string entry = main.FullName;

		if (dir.Exists) {
			foreach (var file in dir.GetFiles("*.cb", SearchOption.AllDirectories)) {
				if (file.FullName == main.FullName) continue;

				files.Add(new FileInfo(file.FullName));
			}
		}

		return (entry, files);
	}

	public void RunProgram(FileInfo entry, List<FileInfo> files) {
		foreach (var path in files) {
			RunFile(File.ReadAllText(path.FullName), path);
		}

		RunFile(File.ReadAllText(entry.FullName), entry);
	}

	public void RunBuild(string entry, Dictionary<string, string> files) {
		foreach (var (name, data) in files) {
			if (name != entry && name != ".srcConfig")
				RunFile(data, new FileInfo(name));
		}

		RunFile(files[entry], new FileInfo(entry));
	}
}