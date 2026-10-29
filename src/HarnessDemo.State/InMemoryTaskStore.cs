using HarnessDemo.Abstractions;

namespace HarnessDemo.State;

/// <summary>状态层：任务队列 / 异步任务状态（对应架构图 Task Store）。</summary>
public sealed class InMemoryTaskStore : ITaskStore
{
    private int _counter;

    public Task<string> CreateAsync(string sessionId, string description, CancellationToken ct)
    {
        var id = $"task-{Interlocked.Increment(ref _counter):D4}";
        Console.WriteLine($"[State/TaskStore] 创建任务 {id}：{description}");
        return Task.FromResult(id);
    }

    public Task UpdateStatusAsync(string taskId, string status, string? result, CancellationToken ct)
    {
        Console.WriteLine($"[State/TaskStore] 任务 {taskId} -> {status}");
        return Task.CompletedTask;
    }
}
