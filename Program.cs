using Cranberry.Packager;

if (File.Exists("source.crpkg")) {
	var (entry, files) = CrpkgZip.ReadPackage("source.crpkg");
	var program = new Cranberry.Program();

	if (File.Exists("include.crpkg")) {
		var (_, includes) = CrpkgZip.ReadPackage("include.crpkg", false);
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
	var program = new Cranberry.Program();
	var (entry, files) = program.CollectFiles(args.Length > 1 ? args[1] : "main.cb");
	files.Add(entry);

	CrpkgZip.Build(entry, files.ToArray(), Config.GetConfig() ?? Config.Default());
} else {
	Console.WriteLine($"`{args[0]}` is not a valid cranberry command.");
}