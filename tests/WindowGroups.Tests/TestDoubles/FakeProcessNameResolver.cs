using WindowGroups.Core.Interop;

namespace WindowGroups.Tests.TestDoubles;

/// <summary>In-memory <see cref="IProcessNameResolver"/> for tests.</summary>
internal sealed class FakeProcessNameResolver : IProcessNameResolver
{
    private readonly Dictionary<int, string> _names = new();

    public void Add(int processId, string name) => _names[processId] = name;

    public string? GetProcessName(int processId) =>
        _names.TryGetValue(processId, out var name) ? name : null;
}