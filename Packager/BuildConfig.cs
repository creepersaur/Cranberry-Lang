using Tomlyn;
using Tomlyn.Model;

namespace Cranberry.Packager;

public struct BuildConfig(TomlTable package) {
	public readonly string Profile = (string)package["profile"];
	public readonly string Name = (string)package["name"];
	public readonly string Version = (string)package["version"];
	public readonly string[] Include = ((TomlArray)package["include"]).Select(x => (string)x!).ToArray();
}

public abstract class Config {
	public static BuildConfig? GetConfig() {
		if (File.Exists("cranberry.toml")) {
			TomlTable model = Toml.Parse(File.ReadAllText("cranberry.toml")).ToModel();

			var package = (TomlTable)model["package"];
			return new BuildConfig(package);
		}

		return null;
	}

	public static BuildConfig Default(bool is_release) {
		return new BuildConfig(new TomlTable {
			["name"] = "executable",
			["version"] = "1.0.0",
			["profile"] = is_release ? "release" : "debug",
			["include"] = new TomlArray(),
		});
	}
}