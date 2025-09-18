var program = new Cranberry.Program();

var files = program.CollectFiles();
program.RunProgram(files.Item1, files.Item2);