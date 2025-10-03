using System.Diagnostics;
using Cranberry.Builtin;
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
		program.RunProgram(files.Item1, files.Item2);
	}
	
	public static void RunFile(string path) {
		var program = new Program(false);
		var files = program.CollectFiles(path);
		program.RunProgram(files.Item1, files.Item2);
	}

	public static void Build(List<string> args) {
		var stopwatch = new Stopwatch();
		stopwatch.Restart();
	
		Console.WriteLine("Trying to build cranberry project.");

		string entry_point = args.Count > 0 ? args[0] : "main.cb";
		Console.WriteLine($"Entry Point: {entry_point}");
	
		var program = new Program(true);
		var (entry, files) = program.CollectFiles(entry_point);
		files.Add(entry);
	
		Console.WriteLine($"Collected files: {Misc.FormatValue(files, true)}");
	
		bool is_release = args.Contains("--release");
		CrpkgZip.Build(entry, files.ToArray(), Config.GetConfig() ?? Config.Default(is_release));

		if (!is_release) {
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("[ DEBUG ]");
		} else {
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine("[ RELEASE ]");
		}
	
		stopwatch.Stop();
		Console.BackgroundColor = ConsoleColor.Green;
		Console.ForegroundColor = ConsoleColor.Black;

		Console.WriteLine($"Build Completed Successfully in {stopwatch.Elapsed.TotalSeconds}s.");
		Console.ResetColor();
	}

	public static void Init(string dir) {
		Directory.CreateDirectory($"{dir}src");
		File.WriteAllText($"{dir}src/main.cb", "println(\"Hello World\")");
		File.WriteAllText($"{dir}cranberry.toml", """
		                                          #################################
		                                          # Cranberry.toml
		                                          ##################################

		                                          [package]
		                                          name = "Program"        # build executable name
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
}