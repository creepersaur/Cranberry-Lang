using System.IO.Compression;
using System.Text;

namespace Cranberry.Packager;

public static class CrpkgZip {
	private static string BuildDir = "build/debug";

	public static void Pack(string entryPoint, string[] inputFilePaths, string[] includeFilePaths, bool is_release) {
		var compression = is_release switch {
			true => CompressionLevel.SmallestSize,
			false => CompressionLevel.Fastest
		};

		using var fs = new FileStream($"{BuildDir}/source.crpkg", FileMode.Create, FileAccess.Write);
		using var archive = new ZipArchive(fs, ZipArchiveMode.Create);

		// Create a `cranberry.srcConfig`
		var srcConfigEntry = archive.CreateEntry(".srcConfig", compression);
		var srcConfigStream = srcConfigEntry.Open();
		srcConfigStream.Write(Encoding.ASCII.GetBytes(Path.GetFileName(entryPoint)));
		srcConfigStream.Close();

		// PACK MAIN PROGRAM
		foreach (var path in inputFilePaths) {
			var entry = archive.CreateEntry(Path.GetFullPath(path), compression);
			using var entryStream = entry.Open();
			using var inFile = File.OpenRead(path);
			inFile.CopyTo(entryStream);
		}

		// PACK INCLUDES
		using var fs_include = new FileStream($"{BuildDir}/include.crpkg", FileMode.Create, FileAccess.Write);
		using var archive_include = new ZipArchive(fs_include, ZipArchiveMode.Create);

		foreach (var path in includeFilePaths) {
			var entry = archive_include.CreateEntry(Path.GetFullPath(path), compression);
			using var entryStream = entry.Open();
			using var inFile = File.OpenRead(path);
			inFile.CopyTo(entryStream);
		}
	}

	public static void Build(string entryPoint, string[] inputFilePaths, BuildConfig config) {
		bool is_release = config.Profile == "release";
		if (is_release)
			BuildDir = "build/release";
		
		Console.WriteLine($"Build dir is: {BuildDir}");
		
		if (Directory.Exists(BuildDir))
			Directory.Delete(BuildDir, true);
		Directory.CreateDirectory(BuildDir);

		Pack(entryPoint, inputFilePaths, config.Include, is_release);

		var exe_path = Environment.ProcessPath ?? System.Reflection.Assembly.GetEntryAssembly()!.Location;
		var exe_dir = Path.GetDirectoryName(exe_path);

		File.Copy(exe_path, $"{BuildDir}/{config.Name}.exe");
		File.Copy($"{exe_dir}/Cranberry.dll", $"{BuildDir}/Cranberry.dll");
		File.Copy($"{exe_dir}/Cranberry.runtimeconfig.json", $"{BuildDir}/Cranberry.runtimeconfig.json");
	}

	public static (string?, Dictionary<string, string>) ReadPackage(string crpkgPath, bool is_main = true) {
		using var fs = File.OpenRead(crpkgPath);
		using var archive = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false);

		// Get `cranberry.srcConfig`
		string? configData = null;
		if (is_main) {
			var srcConfig = archive.GetEntry(".srcConfig") ?? throw new FileNotFoundException(".srcConfig");
			using var srcStream = srcConfig.Open();
			using var srcReader = new StreamReader(srcStream, Encoding.UTF8);
			configData = srcReader.ReadToEnd();
		}

		// Read all other files
		var files = new Dictionary<string, string>();
		foreach (var entry in archive.Entries) {
			using var s = entry.Open();
			using var sr = new StreamReader(s, Encoding.UTF8);

			string fileData = sr.ReadToEnd();
			files.Add(entry.Name, fileData);
		}

		return (configData, files);
	}
}