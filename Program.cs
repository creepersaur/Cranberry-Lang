var text = File.ReadAllText("Hello.cranberry");
var program = new Cranberry.Program(text);

program.RunProgram();