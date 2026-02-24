using System.Diagnostics;
using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;
using Cranberry.Packager;

namespace Cranberry;

public static class Commands {
	public static void RunBuild(string exe_dir) {
		var (entry, files) = CrpkgZip.ReadPackage($"{exe_dir}/source.crpkg");
		var program = new Program(true);

		if (File.Exists($"{exe_dir}/include.crpkg")) {
			var (_, includes) = CrpkgZip.ReadPackage($"{exe_dir}/include.crpkg", false);
			foreach (var (key, value) in includes) {
				program.Includes[key] = value;
			}
		}

		program.RunBuild(entry!, files);
	}

	public static void RunProgram() {
		var program = new Program(false);
		var files = program.CollectFiles("src/main.cb");
		program.RunProgram(new FileInfo(files.Item1), files.Item2);
	}

	public static void RunFile(string path) {
		var program = new Program(false);
		var files = program.CollectFiles(path);
		program.RunProgram(new FileInfo(files.Item1), files.Item2);
	}

	public static void Build(List<string> args) {
		bool is_release = args.Contains("--release");
		if (is_release) args.Remove("--release");

		bool is_logged = args.Contains("--log");
		if (is_logged) args.Remove("--log");

		var config = Config.GetConfig() ?? Config.Default(is_release);

		var BuildDir = is_release ? "build/release/" : "build/debug/";
		Console.Write($"Building Cranberry project → ");
		Console.ForegroundColor = ConsoleColor.Cyan;
		Console.WriteLine(TerminalFolderLink(new DirectoryInfo(BuildDir).FullName, BuildDir));
		Console.ResetColor();

		var stopwatch = new Stopwatch();
		stopwatch.Restart();

		string entry_point = args.Count > 0 ? args[0] : new FileInfo("src/main.cb").FullName;
		if (is_logged) Console.WriteLine($"Entry Point: {entry_point}");

		var program = new Program(true);
		var (entry, files) = program.CollectFiles(entry_point);
		files.Add(new FileInfo(entry));

		if (is_logged) Console.WriteLine($"Collected entry_point: {entry_point}");
		if (is_logged) Console.WriteLine($"Collected files: {Misc.FormatValue(files, true)}");

		CrpkgZip.Build(
			entry_point,
			[.. files],
			config,
			is_logged
		);
		stopwatch.Stop();

		var text = $" {(is_release ? "RELEASE" : "DEBUG")} | Built Successfully in {stopwatch.Elapsed.TotalSeconds}s. ";
		Console.WriteLine($"┏{new string('━', text.Length)}┓");

		Console.Write("┃");

		if (!is_release) {
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write(" DEBUG ");
		} else {
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.Write(" RELEASE ");
		}

		Console.ResetColor();
		Console.Write("|");
		Console.ForegroundColor = ConsoleColor.Green;

		Console.Write($" Built Successfully in {stopwatch.Elapsed.TotalSeconds}s. ");
		Console.ResetColor();

		Console.WriteLine("┃");
		Console.WriteLine($"┗{new string('━', text.Length)}┛");
	}

	public static void Init(string dir) {
		var dirName = new DirectoryInfo(Environment.CurrentDirectory).Name;

		Directory.CreateDirectory($"{dir}src");
		File.WriteAllText($"{dir}src/main.cb", "println(\"Hello, World!\")");
		File.WriteAllText($"{dir}cranberry.toml", $"""
		                                          #################################
		                                          # Cranberry.toml
		                                          ##################################

		                                          [package]
		                                          name = "{dirName}"        # build executable name
		                                          version = "1.0.0"
		                                          profile = "debug"

		                                          # What paths to include during the build
		                                          # All files in /src are already included
		                                          [include]
		                                          files = []
		                                          directories = []
		                                          """);
	}

	public static void New(List<string> args) {
		Init($"{args[0]}/");
	}

	public static void RunShell() {
		var program = new Program(false);
		var fileInfo = new FileInfo("<shell>");
		
		Console.ForegroundColor = ConsoleColor.Cyan;
		Console.WriteLine("╔═════════════════════════════════════╗");
		Console.WriteLine("║     Cranberry Interactive Shell     ║");
		Console.WriteLine("║ Type 'exit()' or hit CTRL+C to quit ║");
		Console.WriteLine("╚═════════════════════════════════════╝");
		Console.ResetColor();
		Console.WriteLine();

		Console.CancelKeyPress += (s, e) => {
			e.Cancel = true;
			Environment.Exit(0);
		};

		while (true) {
			var input_lines = new List<string>{Shell.GetInput()};
			Console.WriteLine();
			
			while (Shell.multilineActive) {
				input_lines.Add(Shell.GetInput());
				Console.WriteLine();
			}
			var input = string.Join("\n", input_lines);
			
			if (string.IsNullOrWhiteSpace(input)) continue;

			try {
				var tokens = new Lexer(input, "<shell>", "<shell>").GetTokens();
				var parser = new Parser(tokens.ToArray(), fileInfo);

				while (parser.PeekAhead() != null) {
					if (parser.Check(";") || parser.Check("\n")) {
						parser.Advance();
						continue;
					}

					var node = parser.Parse();
					
					try {
						var result = program.RunNode(node, fileInfo);
						
						if (result != null && !(result is Node)) {
							Console.ForegroundColor = ConsoleColor.Black;
							Console.WriteLine($"{Misc.FormatValue(result, true)}");
							Console.ResetColor();
						}
					} catch (ReturnException e) {
						if (e.Value != null) {
							Console.ForegroundColor = ConsoleColor.Magenta;
							Console.WriteLine($"{Misc.FormatValue(e.Value, true)}");
							Console.ResetColor();
						}
					} catch (RuntimeError e) {
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine($"RuntimeError: {e.Message}");
						Console.ResetColor();
					} catch (ExecutionError e) {
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine($"ExecutionError: {e.Message}");
						Console.ResetColor();
					}

					if (parser.Check(";") || parser.Check("\n")) {
						parser.Advance();
					}
				}
			} catch (ParseError e) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"ParseError: {e.Message}");
				Console.ResetColor();
			} catch (Exception e) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"Error: {e.Message}");
				Console.ResetColor();
			}
		}
	}

	public static string TerminalFolderLink(string caption, string url) {
		// The sequence is ^]8;;{url}^a{caption}^]8;;^a (using actual escape characters)
		// In C#, you can use Unicode escape sequences: \u001B is the escape character
		return $"\u001B]8;;file://{caption}\u0007{url}\u001B]8;;\u0007";
	}
}