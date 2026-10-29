using HarnessDemo.Abstractions;

namespace HarnessDemo.Skills;

/// <summary>能力层工具：返回当前本地时间。</summary>
public sealed class TimeTool : ITool
{
    public string Name => "get_time";
    public string Description => "获取服务器当前时间";

    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct)
        => Task.FromResult($"现在是 {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
}
