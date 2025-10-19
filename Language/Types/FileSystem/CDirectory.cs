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
			"path" => Path,
			"name" => Info.Name,
			"full_name" => Info.FullName,
			"parent" => new CDirectory(Info.Parent!.FullName),
			"exists" => Info.Exists,

			"create" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`Directory.create()` expects 0 arguments.");

				Directory.CreateDirectory(Path);
				
				return new NullNode();
			}),
			
			"get_files" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`Directory.get_files()` expects 0 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"Directory does not exist at path `{Path}`.");

				var files = Info.GetFiles();
				return new CList(files.Select(object (x) => new CFile(x.FullName)).ToList());
			}),
			
			"get_directories" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`Directory.get_directories()` expects 0 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"Directory does not exist at path `{Path}`.");

				var files = Info.GetDirectories();
				return new CList(files.Select(object (x) => new CDirectory(x.FullName)).ToList());
			}),
			
			"clear" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`Directory.clear()` expects 0 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"Directory does not exist at path `{Path}`.");

				Directory.Delete(Path, recursive: true);
				Directory.CreateDirectory(Path);
				
				return new NullNode();
			}),

			"file_count" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`Directory.file_count()` expects 0 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"Directory does not exist at path `{Path}`.");

				return (double)Info.GetFiles().Length;
			}),

			"delete" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`Directory.delete()` expects 0 arguments.");

				Directory.Delete(Path, recursive: true);

				return new NullNode();
			}),

			"move_to" => new InternalFunction(args => {
				if (args.Length != 1)
					throw new RuntimeError("`Directory.move_to(path)` expects 1 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"Directory does not exist at path `{Path}`.");
				
				if (args[0] is not string new_path)
					throw new RuntimeError("`Directory.move_to()` expects a `string` argument.");
				
				try {
					Directory.Move(Path, System.IO.Path.Combine(new_path, Info.Name));
					Path = new_path;
					Info = new DirectoryInfo(Path);
				} catch {
					throw new RuntimeError($"Could not move directory: `{Path}` to `{new_path}`.");
				}

				return new NullNode();
			}),

			"rename" => new InternalFunction(args => {
				if (args.Length != 1)
					throw new RuntimeError("`Directory.rename(new_name: string)` expects 1 argument.");

				var newName = args[0]!.ToString();
				if (string.IsNullOrWhiteSpace(newName))
					throw new RuntimeError("`Directory.rename(new_name: string)` requires a valid non-empty name.");

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

			"get_attributes" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`Directory.get_attributes()` expects 0 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"Directory does not exist at path `{Path}`.");

				var attrs = new CDict(new Dictionary<object, object> {
					["read_only"] = (double)((Info.Attributes & FileAttributes.ReadOnly) != 0 ? 1 : 0),
					["hidden"] = (double)((Info.Attributes & FileAttributes.Hidden) != 0 ? 1 : 0),
					["archive"] = (double)((Info.Attributes & FileAttributes.Archive) != 0 ? 1 : 0),
					["creation_time"] = Info.CreationTime.ToOADate(),
					["last_write_time"] = Info.LastWriteTime.ToOADate()
				});

				return attrs;
			}),

			"set_read_only" => new InternalFunction(args => {
				if (args.Length != 1)
					throw new RuntimeError("`Directory.set_read_only(flag: bool)` expects 1 argument.");

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