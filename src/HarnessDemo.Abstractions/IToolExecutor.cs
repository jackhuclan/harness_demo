namespace HarnessDemo.Abstractions;

/// <summary>一次工具执行结果（执行层返回给 Controller）。</summary>
public sealed record ToolResult(bool Success, string Output, string? Error = null);

/// <summary>
/// 执行层：工具执行器（对应架构图「执行层 / Tool Executor」）。
/// 负责工具注册表、超时、异常捕获与重试，对 Agent 屏蔽底层差异。
/// </summary>
public interface IToolExecutor
{
    IReadOnlyList<ITool> RegisteredTools { get; }

    Task<ToolResult> ExecuteAsync(string toolName, string argsJson, CancellationToken ct);
}
