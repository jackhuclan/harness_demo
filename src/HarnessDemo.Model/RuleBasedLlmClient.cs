using System.Text.RegularExpressions;
using HarnessDemo.Abstractions;

namespace HarnessDemo.Model;

/// <summary>
/// 模型层：规则模拟的 LLM 客户端（对应架构图「大语言模型 / GPT·Claude·文心」）。
/// 生产环境替换为 OpenAI / 通义 / 文心 SDK 即可，接口不变。
/// 它扮演 Planner：阅读上下文，决定是「调用工具」还是「直接回答」。
/// </summary>
public sealed class RuleBasedLlmClient : ILlmClient
{
    public Task<LlmDecision> DecideAsync(
        IReadOnlyList<ChatMessage> context,
        IReadOnlyList<ITool> availableTools,
        CancellationToken ct)
    {
        // 闭环信号：上一条消息是工具结果 -> 不再调用工具，直接把结果整理为最终答复
        // （真实 function calling 中，LLM 看到 tool message 后自然停止发起新调用）
        var last = context[^1];
        if (last.Role == Role.Tool)
        {
            Console.WriteLine("[Model/Planner] 已收到工具结果，结束工具循环，生成最终答复");
            return Task.FromResult(new LlmDecision(false, null, null, $"[工具结果] {last.Content}"));
        }

        var lastUser = context.LastOrDefault(m => m.Role == Role.User)?.Content ?? "";

        // 意图 1：算术计算 -> 调用 calculator 工具
        var calc = Regex.Match(
            lastUser,
            @"(?:计算|calculate|calc)\s*[:：]?\s*(?<expr>[\d+\-*/().\s]+)",
            RegexOptions.IgnoreCase);
        if (calc.Success)
        {
            var expr = calc.Groups["expr"].Value.Trim();
            Console.WriteLine($"[Model/Planner] 识别为计算意图 -> 调用工具 calculator(\"{expr}\")");
            return Task.FromResult(new LlmDecision(
                true, "calculator",
                System.Text.Json.JsonSerializer.Serialize(new { expression = expr }),
                null));
        }

        // 意图 2：时间查询 -> 调用 get_time 工具
        if (Regex.IsMatch(lastUser, @"(几点|现在.*时间|time|now)", RegexOptions.IgnoreCase))
        {
            Console.WriteLine("[Model/Planner] 识别为时间意图 -> 调用工具 get_time()");
            return Task.FromResult(new LlmDecision(true, "get_time", "{}", null));
        }

        // 意图 3：兜底 -> 直接生成最终答复
        var tail = string.IsNullOrEmpty(lastUser) ? "（历史消息已超出压缩窗口）" : $"「{lastUser}」";
        var reply =
            $"[Mock-LLM] 已收到你的消息{tail}。当前上下文 {context.Count} 条，" +
            $"可用工具 {availableTools.Count} 个。我是运行在 Agent Harness 上的智能体。";
        Console.WriteLine("[Model/Planner] 无需工具，直接生成最终答复");
        return Task.FromResult(new LlmDecision(false, null, null, reply));
    }

    public async IAsyncEnumerable<StreamEvent> StreamDecideAsync(
        IReadOnlyList<ChatMessage> context,
        IReadOnlyList<ITool> availableTools,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var d = await DecideAsync(context, availableTools, ct);
        if (d.NeedsTool)
        {
            yield return new StreamDone(true, d.ToolName, d.ToolArgsJson);
            yield break;
        }
        // 模拟逐字输出
        var text = d.FinalText ?? "";
        for (var i = 0; i < text.Length; i += 3)
        {
            yield return new TextDelta(text.Substring(i, Math.Min(3, text.Length - i)));
            await Task.Delay(20, ct);
        }
        yield return new StreamDone(false, null, null);
    }
}
