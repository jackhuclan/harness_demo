using System.Text.RegularExpressions;
using HarnessDemo.Abstractions;

namespace HarnessDemo.Model;

/// <summary>
/// 模型层：规则模拟的 Planner。两阶段决策：
/// 1) 先路由到哪个 Agent（recipe / pda / tma / tdi / intro）
/// 2) 再在该 Agent 的工具子集里选具体工具
/// </summary>
public sealed class RuleBasedLlmClient : ILlmClient
{
    // 第一阶段：关键词 -> Agent
    private static (string pattern, string agent)[] AgentRoutes = new[]
    {
        (@"(建方|建处方|创建处方|recipe.*create|load.*foup|加载.*硅片|wafer.*load|处方建立)", "recipe"),
        (@"(分析.*处方|处方.*分析|recipe.*analy|优化处方|recipe.*optim)", "recipe"),
        (@"(统计|stats|3σ|lot.*批|异常|fail|分解|budget|归因|attribution|建议|advice|补偿|compensat|R2R|链路|chain)", "pda"),
        (@"(性能|tool performance|重复性|趋势|trend|光源|硬件|寿命|衰减)", "tma"),
        (@"(诊断|报错|error|focus|聚焦|对准|处置|复测|经验|反馈|learn)", "tdi"),
    };

    // 第二阶段：Agent -> 该 Agent 内的工具选择
    private static Dictionary<string, (string pattern, string tool, string args)[]> ToolRoutes = new()
    {
        ["recipe"] = new[]
        {
            (@"(建方|建处方|创建处方|recipe.*create)", "recipe_create", "{}"),
            (@"(load|加载|foup|slot|上片|wafer.*load)", "wafer_load", "{\"foup\":\"Foup2\",\"slot\":5}"),
            (@"(优化|optimize|MRO|self-calibration)", "recipe_optimize", "{}"),
            (@"(自定义技能|习得规则|custom.*skill|动态采样)", "custom_skill_learn", "{}"),
            (@"(分析.*处方|处方.*分析|recipe.*analy)", "recipe_analyze", "{}"),
        },
        ["pda"] = new[]
        {
            (@"(统计|stats|3σ)", "ovl_statistics", "{}"),
            (@"(异常|fail|根因|为什么.*fail)", "ovl_anomaly", "{}"),
            (@"(分解|budget|贡献项)", "budget_decomp", "{}"),
            (@"(归因|attribution|边缘|field-to-field)", "attribution", "{}"),
            (@"(建议|改进|advice)", "decision_advice", "{}"),
            (@"(补偿|compensat|R2R|下发)", "r2r_comp", "{}"),
            (@"(链路|chain|完整|怎么样|如何)", "ovl_chain", "{}"),
        },
        ["tma"] = new[]
        {
            (@"(趋势|trend|近.*周|近.*月)", "tool_trend", "{}"),
            (@"(光源|硬件|寿命|衰减|PM)", "hardware_health", "{}"),
            (@"(性能|tool performance|重复性|MAM|TIS)", "tool_performance", "{}"),
        },
        ["tdi"] = new[]
        {
            (@"(确认|执行处置|复测|confirm)", "disposition_confirm", "{}"),
            (@"(经验|反馈|习得|learn)", "experience_learn", "{}"),
            (@"(诊断|报错|error|focus|聚焦|对准)", "ovl_diagnose", "{\"error\":\"粗聚焦失败\"}"),
        },
        ["intro"] = Array.Empty<(string, string, string)>(),
    };

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
        foreach (var (pat, ag) in AgentRoutes)
        {
            if (Regex.IsMatch(lastUser, pat, RegexOptions.IgnoreCase)) { agent = ag; break; }
        }
        Console.WriteLine($"[Model/Planner] 路由到 Agent: {agent}");

        // 第二阶段：在该 Agent 的工具集里选
        if (ToolRoutes.TryGetValue(agent, out var routes))
        {
            foreach (var (pat, tool, args) in routes)
            {
                if (Regex.IsMatch(lastUser, pat, RegexOptions.IgnoreCase))
                {
                    Console.WriteLine($"[Model/Planner] -> {agent}/{tool}");
                    return Task.FromResult(new LlmDecision(true, tool, args, null, agent));
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
