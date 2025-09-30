using System.Diagnostics;
using Cranberry.Builtin;
using Cranberry.Packager;

var exe_path = Environment.ProcessPath ?? System.Reflection.Assembly.GetEntryAssembly()!.Location;
var exe_dir = Path.GetDirectoryName(exe_path);

if (File.Exists($"{exe_dir}/source.crpkg")) {
	var (entry, files) = CrpkgZip.ReadPackage($"{exe_dir}/source.crpkg");
	var program = new Cranberry.Program();

	if (File.Exists($"{exe_dir}/include.crpkg")) {
		var (_, includes) = CrpkgZip.ReadPackage($"{exe_dir}/include.crpkg", false);
		foreach (var (key, value) in includes) {
			program.Includes[key] = value;
		}
	}

	program.RunBuild(entry!, files);

	return;
}

if (args.Length < 1) {
	Console.WriteLine("Use `cranberry run` or `cranberry build`.");
	return;
}

if (args[0] == "run") {
	var program = new Cranberry.Program();
	var files = program.CollectFiles(args.Length > 1 ? args[1] : "main.cb");
	program.RunProgram(files.Item1, files.Item2);
} else if (args[0] == "build") {
	var stopwatch = new Stopwatch();
	stopwatch.Restart();
	
	Console.WriteLine("Trying to build cranberry project.");

	string entry_point = args.Length > 1 ? args[1] : "main.cb";
	Console.WriteLine($"Entry Point: {entry_point}");
	
	var program = new Cranberry.Program();
	var (entry, files) = program.CollectFiles(entry_point);
	files.Add(entry);
	
	Console.WriteLine($"Collected files: {Misc.FormatValue(files, true)}");
	
	bool is_release = args.Contains("--release");
	CrpkgZip.Build(entry, files.ToArray(), Config.GetConfig() ?? Config.Default(is_release));

	Console.ForegroundColor = ConsoleColor.Green;
	Console.Write("Successfully built the Cranberry project ");

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

	Console.WriteLine($"Build Completed Sucessfully in {stopwatch.Elapsed.TotalSeconds}s.");
	Console.ResetColor();
} else {
	Console.WriteLine($"`{args[0]}` is not a valid cranberry command.");
}