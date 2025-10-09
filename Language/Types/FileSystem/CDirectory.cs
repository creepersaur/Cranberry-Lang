using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CDirectory(string path) : IMemberAccessible {
	public string Path = path;
	public DirectoryInfo Info = new(path);

	public override string ToString() => $"Directory({Path})";

	public object GetMember(object? member) {
		if (member is not string)
			throw new RuntimeError($"Tried to get member of unsupported datatype `{member}` on Directory.");

		return member switch {
			"Path" => Path,
			"Name" => Info.Name,
			"FullName" => Info.FullName,
			"Parent" => new CDirectory(Info.Parent!.FullName),

			"Create" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`Directory.Create()` expects 0 arguments.");

				Directory.CreateDirectory(Path);
				
				return new NullNode();
			}),
			
			"GetFiles" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`Directory.GetFiles()` expects 0 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"Directory does not exist at path `{Path}`.");

				var files = Info.GetFiles();
				return new CList(files.Select(object (x) => new CFile(x.FullName)).ToList());
			}),
			
			"GetDirectories" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`Directory.GetDirectories()` expects 0 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"Directory does not exist at path `{Path}`.");

				var files = Info.GetDirectories();
				return new CList(files.Select(object (x) => new CDirectory(x.FullName)).ToList());
			}),
			
			"Clear" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`Directory.Clear()` expects 0 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"Directory does not exist at path `{Path}`.");

				Directory.Delete(Path, recursive: true);
				Directory.CreateDirectory(Path);
				
				return new NullNode();
			}),

			"FileCount" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`Directory.FileCount()` expects 0 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"Directory does not exist at path `{Path}`.");

				return (double)Info.GetFiles().Length;
			}),

			"Exists" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`Directory.Exists()` expects 0 arguments.");

				return Info.Exists;
			}),

			"Delete" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`Directory.Delete()` expects 0 arguments.");

				Directory.Delete(Path, recursive: true);

				return new NullNode();
			}),

			"MoveTo" => new InternalFunction(args => {
				if (args.Length != 1)
					throw new RuntimeError("`Directory.MoveTo(path)` expects 1 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"Directory does not exist at path `{Path}`.");
				
				if (args[0] is not string new_path)
					throw new RuntimeError("`Directory.MoveTo()` expects a `string` argument.");
				
				try {
					Directory.Move(Path, System.IO.Path.Combine(new_path, Info.Name));
					Path = new_path;
					Info = new DirectoryInfo(Path);
				} catch {
					throw new RuntimeError($"Could not move directory: `{Path}` to `{new_path}`.");
				}

				return new NullNode();
			}),

			"Rename" => new InternalFunction(args => {
				if (args.Length != 1)
					throw new RuntimeError("`Directory.Rename(new_name: string)` expects 1 argument.");

				var newName = args[0]!.ToString();
				if (string.IsNullOrWhiteSpace(newName))
					throw new RuntimeError("`Directory.Rename(new_name: string)` requires a valid non-empty name.");

				if (!Info.Exists)
					throw new RuntimeError($"Directory does not exist at path `{Path}`.");
				
				if (Info.Parent is null)
					throw new RuntimeError("Could not determine parent directory for rename.");

				var dir = Info.Parent!.FullName;
				var newPath = System.IO.Path.Combine(dir, newName);
				
				Directory.Move(Path, newPath);

				Path = newPath;
				Info = new DirectoryInfo(newPath);

				return new NullNode();
			}),

			"GetAttributes" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`Directory.GetAttributes()` expects 0 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"Directory does not exist at path `{Path}`.");

				var attrs = new CDict(new Dictionary<object, object> {
					["ReadOnly"] = (double)((Info.Attributes & FileAttributes.ReadOnly) != 0 ? 1 : 0),
					["Hidden"] = (double)((Info.Attributes & FileAttributes.Hidden) != 0 ? 1 : 0),
					["Archive"] = (double)((Info.Attributes & FileAttributes.Archive) != 0 ? 1 : 0),
					["CreationTime"] = Info.CreationTime.ToOADate(),
					["LastWriteTime"] = Info.LastWriteTime.ToOADate()
				});

				return attrs;
			}),

			"SetReadOnly" => new InternalFunction(args => {
				if (args.Length != 1)
					throw new RuntimeError("`Directory.SetReadOnly(flag: bool)` expects 1 argument.");

				if (!Info.Exists)
					throw new RuntimeError($"Directory does not exist at path `{Path}`.");

				bool readOnly = Misc.IsTruthy(args[0]!);
				
				var attrs = Info.Attributes;
				if (readOnly) attrs |= FileAttributes.ReadOnly;
				else attrs &= ~FileAttributes.ReadOnly;

				Info.Attributes = attrs;

				return new NullNode();
			}),

			_ => throw new RuntimeError($"Tried to get unknown member `{member}` on Directory.")
		};
	}
}