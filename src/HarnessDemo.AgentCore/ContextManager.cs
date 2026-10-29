using HarnessDemo.Abstractions;

namespace HarnessDemo.AgentCore;

/// <summary>
/// 上下文管理器（对应架构图 Agent Core / Context 上下文管理）。
/// 职责：上下文组装（System 提示词 + 历史）、动态压缩（只保留最近 N 条）、Token 预算控制。
/// </summary>
public sealed class ContextManager
{
    private const int MaxHistory = 10;

    private static readonly string SystemPrompt =
        "你是运行在 Agent Harness 平台上的智能体。平台分层：入口层 / 会话路由层 / " +
        "Agent Core（Planner·Controller·Memory·Context）/ 能力层 / 执行层 / 状态层 / 模型层 / 安全层。" +
        "需要外部能力时通过执行层 ToolExecutor 调用工具。";

    public IReadOnlyList<ChatMessage> Build(IReadOnlyList<ChatMessage> history)
    {
        var list = new List<ChatMessage> { new(Role.System, SystemPrompt) };

        // 动态压缩：超出窗口只保留最近 MaxHistory 条（对应 Context 的动态压缩/Token 管理）
        var recent = history.Count <= MaxHistory
            ? history
            : history.TakeLast(MaxHistory).ToList();
        list.AddRange(recent);

        Console.WriteLine($"[AgentCore/Context] 组装上下文 {list.Count} 条（历史 {history.Count} 条，压缩窗口 {MaxHistory}）");
        return list;
    }
}
