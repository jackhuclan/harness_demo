using HarnessDemo.Abstractions;

namespace HarnessDemo.AgentCore;

/// <summary>
/// Agent Controller（对应架构图 Agent Core 核心引擎）。
/// 这是 Harness 的心脏：Planner(模型决策) -> Controller(执行循环) -> 工具结果回灌 -> 再决策，
/// 直到模型给出最终答复。带最大跳数保护，防止工具调用死循环。
/// </summary>
public sealed class Agent
{
    private const int MaxHops = 6;

    private readonly ILlmClient _llm;
    private readonly IToolExecutor _executor;
    private readonly ISessionStore _sessions;
    private readonly IMemoryStore _memory;
    private readonly IContentAuditor _auditor;
    private readonly ContextManager _context;

    public Agent(
        ILlmClient llm,
        IToolExecutor executor,
        ISessionStore sessions,
        IMemoryStore memory,
        IContentAuditor auditor,
        ContextManager context)
    {
        _llm = llm;
        _executor = executor;
        _sessions = sessions;
        _memory = memory;
        _auditor = auditor;
        _context = context;
    }

    public async Task<string> RunAsync(string sessionId, string userInput, CancellationToken ct = default)
    {
        // 1. 安全层闸门（内容安全前置）
        var audit = await _auditor.AuditAsync(userInput, ct);
        if (!audit.Passed)
            return $"⛔ {audit.Reason}";

        // 2. 状态层：加载会话历史
        var session = await _sessions.GetOrCreateAsync(sessionId, ct);
        session.Messages.Add(new ChatMessage(Role.User, userInput));

        var finalAnswer = "(模型未给出最终答复)";

        // 3. Controller 执行循环：Planner 决策 -> 工具执行 -> 结果回灌
        for (var hop = 1; hop <= MaxHops; hop++)
        {
            Console.WriteLine($"[AgentCore/Controller] --- 第 {hop} 轮决策 ---");

            var context = _context.Build(session.Messages);
            var decision = await _llm.DecideAsync(context, _executor.RegisteredTools, ct);

            if (decision.NeedsTool && decision.ToolName is not null)
            {
                // 4. 执行层：调度工具
                var result = await _executor.ExecuteAsync(decision.ToolName, decision.ToolArgsJson ?? "{}", ct);

                // 5. 工具结果回灌上下文，进入下一轮
                session.Messages.Add(new ChatMessage(Role.Assistant,
                    $"调用工具 {decision.ToolName}", decision.ToolName));
                session.Messages.Add(new ChatMessage(Role.Tool,
                    result.Success ? result.Output : $"执行失败：{result.Error}", decision.ToolName));

                finalAnswer = result.Success ? result.Output : $"工具执行失败：{result.Error}";
                continue;
            }

            // 6. 模型给出最终答复
            finalAnswer = decision.FinalText ?? finalAnswer;
            break;
        }

        // 7. 持久化：会话历史 + 长期记忆
        session.Messages.Add(new ChatMessage(Role.Assistant, finalAnswer));
        await _sessions.SaveAsync(session, ct);
        await _memory.AppendAsync(sessionId, "last_answer", finalAnswer, ct);

        return finalAnswer;
    }
}
