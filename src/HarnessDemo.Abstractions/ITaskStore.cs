namespace HarnessDemo.Abstractions;

/// <summary>状态层：任务队列 / 异步任务状态（Task Store）。</summary>
public interface ITaskStore
{
    Task<string> CreateAsync(string sessionId, string description, CancellationToken ct);
    Task UpdateStatusAsync(string taskId, string status, string? result, CancellationToken ct);
}
