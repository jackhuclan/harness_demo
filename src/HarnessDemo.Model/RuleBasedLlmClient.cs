using System.Text.RegularExpressions;
using HarnessDemo.Abstractions;
using Microsoft.Extensions.Options;

namespace HarnessDemo.Model;

/// <summary>
/// 模型层：规则模拟的 Planner。两阶段决策：
/// 1) 先路由到哪个 Agent（recipe / pda / tma / tdi / intro）
/// 2) 再在该 Agent 的工具子集里选具体工具
/// 路由规则从 AgentRouteOptions 配置注入。
/// </summary>
public sealed class RuleBasedLlmClient : ILlmClient
{
    private readonly AgentRouteOptions _options;

    public RuleBasedLlmClient(IOptions<AgentRouteOptions> options)
    {
        _options = options.Value;
    }

    public Task<LlmDecision> DecideAsync(
        IReadOnlyList<ChatMessage> context,
        IReadOnlyList<ITool> availableTools,
        CancellationToken ct)
    {
        // 闭环：上一条是工具结果 -> 直接整理为最终答复
        var last = context[^1];
        if (last.Role == Role.Tool)
        {
            Console.WriteLine("[Model/Planner] 工具结果回灌，结束循环");
            return Task.FromResult(new LlmDecision(false, null, null, $"[工具结果] {last.Content}"));
        }

        var lastUser = context.LastOrDefault(m => m.Role == Role.User)?.Content ?? "";

        // 第一阶段：选 Agent
        var agent = "intro";
        foreach (var r in _options.AgentRoutes)
        {
            if (Regex.IsMatch(lastUser, r.Pattern, RegexOptions.IgnoreCase)) { agent = r.Agent; break; }
        }
        Console.WriteLine($"[Model/Planner] 路由到 Agent: {agent}");

        // 第二阶段：在该 Agent 的工具集里选
        if (_options.ToolRoutes.TryGetValue(agent, out var routes))
        {
            foreach (var r in routes)
            {
                if (Regex.IsMatch(lastUser, r.Pattern, RegexOptions.IgnoreCase))
                {
                    Console.WriteLine($"[Model/Planner] -> {agent}/{r.Tool}");
                    return Task.FromResult(new LlmDecision(true, r.Tool, r.Args, null, agent));
                }
            }
        }

        // intro Agent 或未匹配 -> 直接回复
        if (Regex.IsMatch(lastUser, @"你好|介绍|hello|hi|你是谁", RegexOptions.IgnoreCase))
        {
            var intro = "我是 OVL Agent Harness，覆盖 5 个智能体：\n" +
                "- 处方管理：建方、加载硅片、处方分析、优化\n" +
                "- 生产数据分析：统计、异常、误差分解、归因、R2R\n" +
                "- 机台监测分析：性能、趋势、硬件健康\n" +
                "- 机台诊断修复：报错诊断、处置、经验学习\n" +
                "试试：「分析下 ProductA_M1M2_Layer 这个处方」";
            return Task.FromResult(new LlmDecision(false, null, null, intro, "intro"));
        }

        // 兜底：计算/时间
        var calc = Regex.Match(lastUser, @"(?:计算|calc)\s*[:：]?\s*(?<expr>[\d+\-*/().\s]+)", RegexOptions.IgnoreCase);
        if (calc.Success)
            return Task.FromResult(new LlmDecision(true, "calculator",
                System.Text.Json.JsonSerializer.Serialize(new { expression = calc.Groups["expr"].Value.Trim() }), null, "intro"));
        if (Regex.IsMatch(lastUser, @"(几点|现在.*时间|time|now)", RegexOptions.IgnoreCase))
            return Task.FromResult(new LlmDecision(true, "get_time", "{}", null, "intro"));

        return Task.FromResult(new LlmDecision(false, null, null,
            $"[Agent:{agent}] 我已收到你的消息，请描述你想做的操作（建方/分析/诊断/监测等）。", agent));
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
        var text = d.FinalText ?? "";
        for (var i = 0; i < text.Length; i += 3)
        {
            yield return new TextDelta(text.Substring(i, Math.Min(3, text.Length - i)));
            await Task.Delay(20, ct);
        }
        yield return new StreamDone(false, null, null);
    }
}
