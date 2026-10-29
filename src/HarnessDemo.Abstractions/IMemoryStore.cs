namespace HarnessDemo.Abstractions;

/// <summary>状态层：长期记忆 / 用户画像（Memory Store）。</summary>
public interface IMemoryStore
{
    Task AppendAsync(string userId, string key, string value, CancellationToken ct);

    /// <summary>按查询召回相关记忆（Demo 中简化为前缀过滤）。</summary>
    Task<IReadOnlyList<string>> RecallAsync(string userId, string query, CancellationToken ct);
}
