// Cranberry/External/IExternalObject.cs
namespace Cranberry.External;

/// <summary>
/// Marker for CLR-backed Cranberry wrappers that expose the original CLR instance.
/// </summary>
public interface IExternalObject
{
	/// <summary>
	/// The underlying CLR object this wrapper holds.
	/// </summary>
	public object Internal { get; }
}