using Cranberry;

var exe_path = Environment.ProcessPath ?? System.Reflection.Assembly.GetEntryAssembly()!.Location;
var exe_dir = Path.GetDirectoryName(exe_path);

if (File.Exists($"{exe_dir}/source.crpkg")) {
	if (exe_dir != null) Commands.RunBuild(exe_dir);
	return;
}

if (args.Length < 1 || args[0] == "help" || args[0] == "-h") {
	Console.WriteLine("----------------------");
	Console.WriteLine("|  Cranberry - Lang  |");
	Console.WriteLine("----------------------");
	Console.WriteLine("");
	Console.WriteLine("COMMANDS:");
	Console.WriteLine(" > help | -h            [Get all* commands in Cranberry]");
	Console.WriteLine(" > --version | -v       [Get the current version*.]");
	Console.WriteLine(" > init                 [Initialize a new project in the working directory]");
	Console.WriteLine(" > new <name>           [Create a new project in a new directory]");
	Console.WriteLine(" > <file> <args>        [Run the project]");
	Console.WriteLine(" > run <args>           [Run the project]");
	Console.WriteLine(" > build                [Build the project into a standalone package (in /build/debug)]");
	Console.WriteLine(" > build --release      [Create a release build with a smaller file size (in /build/release)]");
	//Console.WriteLine(" > --analyze            [Analyze the project and return defined variables, functions, etc.]");
	Console.WriteLine("\n(Anything with an asterisk * may be subject to change or may not work.");
	return;
}

if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v")) {
	Console.WriteLine("Version v0.5.0 (alpha)");
	return;
}

string cmd = args[0];
var Args = args.ToList();
Args.RemoveAt(0);

if (File.Exists(cmd)) {
	Commands.RunFile(cmd);
} else if (cmd == "run") {
	Commands.RunProgram();
} else if (cmd == "build") {
	Commands.Build(Args);
} else if (cmd == "init") {
	Commands.Init("");
} else if (cmd == "new") {
	Commands.New(Args);
} else {
	Console.WriteLine($"`{cmd}` is not a valid cranberry command.");
}