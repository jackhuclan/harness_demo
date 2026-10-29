namespace HarnessDemo.Abstractions;

/// <summary>
/// 模型层决策结果：要么调用工具（Planner 决定），要么直接给出最终答复。
/// 真实环境中这由 LLM 的 function calling 返回；Demo 用规则引擎模拟。
/// </summary>
public sealed record LlmDecision(
    bool NeedsTool,
    string? ToolName,
    string? ToolArgsJson,
    string? FinalText,
    string? AgentName = null);

/// <summary>流式输出事件：文本增量 或 最终决策（工具调用/结束）。</summary>
public abstract record StreamEvent;
public sealed record TextDelta(string Text) : StreamEvent;
public sealed record StreamDone(bool NeedsTool, string? ToolName, string? ToolArgsJson) : StreamEvent;

/// <summary>模型层接口（对应架构图「模型层 / 大语言模型」）。</summary>
public interface ILlmClient
{
    Task<LlmDecision> DecideAsync(
        IReadOnlyList<ChatMessage> context,
        IReadOnlyList<ITool> availableTools,
        CancellationToken ct);

    /// <summary>流式版本：逐段 yield 文本增量，结束时 yield 一个 StreamDone。</summary>
    IAsyncEnumerable<StreamEvent> StreamDecideAsync(
        IReadOnlyList<ChatMessage> context,
        IReadOnlyList<ITool> availableTools,
        CancellationToken ct);
}
