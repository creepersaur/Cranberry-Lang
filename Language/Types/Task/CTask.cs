using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Nodes;

namespace Cranberry.Types;

public class CTask : IMemberAccessible {
    private Task<object?> _task;

    public CTask(Task<object?> task) {
        _task = task;
    }

    public object GetMember(object? member) {
        if (member is string name)
            return name switch {
                "is_completed" => new InternalFunction((_, _) => _task.IsCompleted),

                "wait" => new InternalFunction((_, _) => {
                    _task.Wait();
                    return new NullNode();
                }),

                "result" => new InternalFunction((_, _) => {
                    return _task.Result ?? new NullNode();
                }),

                _ => throw new RuntimeError($"Tried getting unknown member `{member}` on type `CThread`")
            };

        throw new RuntimeError($"Tried getting unknown member `{member}` on type `CThread`");
    }
}