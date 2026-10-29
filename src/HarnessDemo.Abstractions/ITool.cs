namespace HarnessDemo.Abstractions;

/// <summary>能力层工具插件（对应架构图 Tools / Plugins）。</summary>
public interface ITool
{
    /// <summary>工具名，路由键。</summary>
    string Name { get; }

    /// <summary>给 Planner 看的能力描述。</summary>
    string Description { get; }

    /// <summary>执行工具，入参为 JSON 字符串，返回文本结果。</summary>
    Task<string> ExecuteAsync(string argsJson, CancellationToken ct);
}
