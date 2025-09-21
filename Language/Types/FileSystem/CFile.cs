using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CFile(string path) : IMemberAccessible {
	public string Path = path;
	public FileInfo Info = new(path);

	public object GetMember(object? member) {
		if (member is not string)
			throw new RuntimeError("`File` only has string named members.");

		return member switch {
			"Path" => Path,

			"Create" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`File.Create()` expects 0 arguments.");

				File.Create(Path);
				
				return new NullNode();
			}),
			
			"Read" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`File.Read()` expects 0 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"File does not exist at path `{Path}`.");

				return File.ReadAllText(Path);
			}),

			"ReadBytes" => new InternalFunction(args => {
				if (args.Length != 2)
					throw new RuntimeError("`File.ReadBytes(offset: int, count: int)` expects 2 arguments.");

				if (!Misc.TryGetInt(args[0], out int offset) || !Misc.TryGetInt(args[1], out int count))
					throw new RuntimeError("`File.ReadBytes(offset: int, count: int)` expects two integer arguments.");

				if (offset < 0 || count < 0)
					throw new RuntimeError("offset and count must be non-negative integers.");

				using var stream = Info.OpenRead();

				if (!stream.CanSeek)
					throw new RuntimeError("Stream is not seekable.");

				if (offset > stream.Length)
					throw new RuntimeError("Offset is beyond end of file.");

				stream.Position = offset;

				var buffer = new byte[count];
				int read = 0;
				while (read < count) {
					int r = stream.Read(buffer, read, count - read);
					if (r == 0) break;
					read += r;
				}

				if (read < count) {
					Array.Resize(ref buffer, read); // shrink buffer
				}

				return new CList(buffer.Select(b => (object)(double)b).ToList());
			}),

			"Write" => new InternalFunction(args => {
				if (args.Length != 1)
					throw new RuntimeError("`File.Write(text)` expects 1 argument.");

				return File.WriteAllTextAsync(Path, Misc.FormatValue(args[0]!));
			}),

			"Append" => new InternalFunction(args => {
				if (args.Length != 1)
					throw new RuntimeError("`File.Append(text)` expects 1 argument.");

				return File.AppendAllTextAsync(Path, Misc.FormatValue(args[0]!));
			}),

			"WriteBytes" => new InternalFunction(args => {
				if (args.Length != 2)
					throw new RuntimeError("`File.WriteBytes(offset: int, bytes: list[number])` expects 2 arguments.");

				if (!Misc.TryGetInt(args[0], out int fileOffset))
					throw new RuntimeError("`File.WriteBytes(offset: int, bytes: list[number])`\n\t\t   ^^^ expects an integer as the first argument.");

				if (fileOffset < 0)
					throw new RuntimeError("offset must be non-negative.");

				if (args[1] is not CList buffer)
					throw new RuntimeError("`File.WriteBytes(offset: int, bytes: list[number])`\n\t\t   ^^^ expects a list of bytes as the second argument.");

				int count = buffer.Items.Count;
				if (count == 0)
					return new NullNode();

				// convert list items to bytes safely
				var bytes = new byte[count];
				for (int i = 0; i < count; i++) {
					var item = buffer.Items[i];

					double d;
					if (item is double dd) d = dd;
					else if (item is int ii) d = ii;
					else if (item is long ll) d = ll;
					else if (!double.TryParse(item.ToString(), out d))
						throw new RuntimeError($"WriteBytes: buffer item at index {i} is not a number.");

					if (d is < 0 or > 255)
						throw new RuntimeError($"WriteBytes: buffer item at index {i} ({d}) is out of byte range 0..255.");

					bytes[i] = Convert.ToByte(d);
				}

				using var stream = Info.OpenWrite();

				if (!stream.CanSeek)
					throw new RuntimeError("Stream is not seekable; cannot write at a specific file offset.");

				stream.Position = fileOffset;
				stream.Write(bytes, 0, bytes.Length);

				return new NullNode();
			}),

			"Truncate" => new InternalFunction(args => {
				if (args.Length != 1)
					throw new RuntimeError("`File.Truncate(length: int)` expects 1 argument.");

				if (!Info.Exists)
					throw new RuntimeError($"File does not exist at path `{Path}`.");

				if (!Misc.TryGetInt(args[0]!, out var new_length))
					throw new RuntimeError("`File.Truncate(length: int)` expects 1 integer argument.");

				using var stream = Info.Open(FileMode.OpenOrCreate, FileAccess.Write);
				stream.SetLength(new_length);

				return new NullNode();
			}),

			"Length" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`File.Length()` expects 0 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"File does not exist at path `{Path}`.");

				return (double)Info.Length;
			}),

			"Exists" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`File.Exists()` expects 0 arguments.");

				return Info.Exists;
			}),

			"Delete" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`File.Exists()` expects 0 arguments.");

				File.Delete(Path);

				return new NullNode();
			}),

			"MoveTo" => new InternalFunction(args => {
				if (args.Length != 1)
					throw new RuntimeError("`File.MoveTo(path)` expects 1 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"File does not exist at path `{Path}`.");

				try {
					File.Move(Path, args[0]!.ToString()!, Misc.IsTruthy(args[1]));
					Path = args[0]!.ToString()!;
					Info = new FileInfo(Path);
				} catch {
					throw new RuntimeError($"Could not move file: `{Path}`.");
				}

				return new NullNode();
			}),

			"Rename" => new InternalFunction(args => {
				if (args.Length != 1)
					throw new RuntimeError("`File.Rename(new_name: string)` expects 1 argument.");

				var newName = args[0]!.ToString();
				if (string.IsNullOrWhiteSpace(newName))
					throw new RuntimeError("`File.Rename(new_name: string)` requires a valid non-empty name.");

				if (!Info.Exists)
					throw new RuntimeError($"File does not exist at path `{Path}`.");

				var dir = Info.DirectoryName;
				if (dir == null)
					throw new RuntimeError("Could not determine parent directory for rename.");

				var newPath = System.IO.Path.Combine(dir, newName);
				File.Move(Path, newPath);

				Path = newPath;
				Info = new FileInfo(newPath);

				return new NullNode();
			}),

			"GetAttributes" => new InternalFunction(args => {
				if (args.Length != 0)
					throw new RuntimeError("`File.GetAttributes()` expects 0 arguments.");

				if (!Info.Exists)
					throw new RuntimeError($"File does not exist at path `{Path}`.");

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
					throw new RuntimeError("`File.SetReadOnly(flag: bool)` expects 1 argument.");

				if (!Info.Exists)
					throw new RuntimeError($"File does not exist at path `{Path}`.");

				bool readOnly = Misc.IsTruthy(args[0]!);
				
				var attrs = Info.Attributes;
				if (readOnly) attrs |= FileAttributes.ReadOnly;
				else attrs &= ~FileAttributes.ReadOnly;

				Info.Attributes = attrs;

				return new NullNode();
			}),

			_ => throw new RuntimeError($"Tried to get unknown member `{member}` on File.")
		};
	}
}