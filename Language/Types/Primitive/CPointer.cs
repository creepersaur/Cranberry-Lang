using System.Runtime.InteropServices;
using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CPointer : IMemberAccessible, IDisposable {
	private GCHandle _handle;

	public override string ToString() => $"CPointer<0x{Address:X}>";
	public IntPtr Address { get; private set; }
    public bool IsFreed => !_handle.IsAllocated;

    public CPointer(object obj) {
        // Pin the object immediately upon creation
        _handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
        Address = _handle.AddrOfPinnedObject();
    }
	
	public void Free() {
        if (_handle.IsAllocated) {
            _handle.Free();
            Address = IntPtr.Zero;
        }
    }

	public void Dispose() => Free();
	~CPointer() => Free();

	/////////////////////////////////////////////////////////
	// MEMBERSHIP
	///////////////////////////////////////////////////////// 

	public object GetMember(object? member) {
		if (member is string name)
			return name switch {
				"address" => (double)Address,

				"is_freed" => IsFreed, // Handy for users to check!

				"free" => new InternalFunction((_, args) => {
					if (args.Length != 0)
						throw new RuntimeError("`free()` expects 0 arguments.");

					Free();

					return new NullNode();
				}),

				_ => throw new RuntimeError($"Tried getting unknown member `{member}` on type `string`")
			};

		throw new RuntimeError($"Tried getting unknown member `{member}` on type `string`");
	}
}