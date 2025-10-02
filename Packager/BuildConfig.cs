using Tomlyn;
using Tomlyn.Model;

namespace Cranberry.Packager;

public struct BuildConfig(TomlTable package, TomlTable include) {
	public readonly string Profile = (string)package["profile"];
	public readonly string Name = (string)package["name"];
	public readonly string Version = (string)package["version"];
	public readonly string[] IncludeFiles = ((TomlArray)include["files"]).Select(x => (string)x!).ToArray();
	public readonly string[] IncludeDir = ((TomlArray)include["directories"]).Select(x => (string)x!).ToArray();
}

public abstract class Config {
	public static BuildConfig? GetConfig() {
		if (File.Exists("cranberry.toml")) {
			TomlTable model = Toml.Parse(File.ReadAllText("cranberry.toml")).ToModel();

			var package = (TomlTable)model["package"];
			var include = (TomlTable)model["include"];
			return new BuildConfig(package, include);
		}

		return null;
	}

	public static BuildConfig Default(bool is_release) {
		return new BuildConfig(new TomlTable {
			["name"] = "executable",
			["version"] = "1.0.0",
			["profile"] = is_release ? "release" : "debug",
		}, new TomlTable {
			["files"] = new TomlArray(),
			["directories"] = new TomlArray()
		});
	}
}