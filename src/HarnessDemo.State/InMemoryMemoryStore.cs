using HarnessDemo.Abstractions;

namespace HarnessDemo.State;

/// <summary>状态层：长期记忆（内存字典，生产可换向量库做语义召回）。</summary>
public sealed class InMemoryMemoryStore : IMemoryStore
{
    private readonly Dictionary<string, List<(string Key, string Value)>> _store = new();

    public Task AppendAsync(string userId, string key, string value, CancellationToken ct)
    {
        if (!_store.TryGetValue(userId, out var list))
            _store[userId] = list = new List<(string, string)>();
        list.Add((key, value));
        Console.WriteLine($"[State/MemoryStore] 用户 {userId} 记忆新增 {key}（累计 {list.Count} 条）");
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> RecallAsync(string userId, string query, CancellationToken ct)
    {
        if (!_store.TryGetValue(userId, out var list))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        var hits = list.Where(x => x.Value.Contains(query, StringComparison.OrdinalIgnoreCase))
                       .Select(x => x.Value)
                       .ToList();
        return Task.FromResult<IReadOnlyList<string>>(hits);
    }
}
