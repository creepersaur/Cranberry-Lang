var text = File.ReadAllText("Hello.js");
var program = new Cranberry.Program(text);

program.RunProgram();

foreach (var scope in program.interpreter.env.Variables) {
	foreach (KeyValuePair<string, object?> kvp in scope) {
		Console.WriteLine("{0} -> {1}", kvp.Key, kvp.Value);
	}
}