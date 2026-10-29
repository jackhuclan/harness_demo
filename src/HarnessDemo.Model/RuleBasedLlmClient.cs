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

        // --- OVL Agent 意图路由（FR-SYS-001）---
        if (Regex.IsMatch(lastUser, @"(统计|stats|3σ|lot).*(lot|LOT|批)", RegexOptions.IgnoreCase))
        {
            Console.WriteLine("[Model/Planner] 识别为 OVL 数据统计意图 -> ovl_statistics");
            return Task.FromResult(new LlmDecision(true, "ovl_statistics", "{}", null));
        }
        if (Regex.IsMatch(lastUser, @"(fail|失败|异常|根因|为什么.*fail)", RegexOptions.IgnoreCase))
        {
            Console.WriteLine("[Model/Planner] 识别为 OVL 异常分析意图 -> ovl_anomaly");
            return Task.FromResult(new LlmDecision(true, "ovl_anomaly", "{}", null));
        }
        if (Regex.IsMatch(lastUser, @"(分析.*处方|处方.*分析|recipe.*analy)", RegexOptions.IgnoreCase))
        {
            Console.WriteLine("[Model/Planner] 识别为处方分析意图 -> recipe_analyze");
            return Task.FromResult(new LlmDecision(true, "recipe_analyze", "{}", null));
        }
        if (Regex.IsMatch(lastUser, @"(性能|tool performance|重复性|MAM|TIS)", RegexOptions.IgnoreCase))
        {
            Console.WriteLine("[Model/Planner] 识别为机台性能检测意图 -> tool_performance");
            return Task.FromResult(new LlmDecision(true, "tool_performance", "{}", null));
        }
        if (Regex.IsMatch(lastUser, @"(诊断|报错|error|focus|聚焦|对准)", RegexOptions.IgnoreCase))
        {
            Console.WriteLine("[Model/Planner] 识别为机台诊断意图 -> ovl_diagnose");
            return Task.FromResult(new LlmDecision(true, "ovl_diagnose", "{\"error\":\"粗聚焦失败\"}", null));
        }
        if (Regex.IsMatch(lastUser, @"(load|加载|foup|slot|上片)", RegexOptions.IgnoreCase))
        {
            Console.WriteLine("[Model/Planner] 识别为硅片加载意图 -> wafer_load");
            return Task.FromResult(new LlmDecision(true, "wafer_load", "{\"foup\":\"Foup2\",\"slot\":5}", null));
        }
        if (Regex.IsMatch(lastUser, @"(分解|budget|贡献项)", RegexOptions.IgnoreCase))
            return Task.FromResult(new LlmDecision(true, "budget_decomp", "{}", null));
        if (Regex.IsMatch(lastUser, @"(归因|attribution|field-to-field|边缘)", RegexOptions.IgnoreCase))
            return Task.FromResult(new LlmDecision(true, "attribution", "{}", null));
        if (Regex.IsMatch(lastUser, @"(建议|改进|advice)", RegexOptions.IgnoreCase))
            return Task.FromResult(new LlmDecision(true, "decision_advice", "{}", null));
        if (Regex.IsMatch(lastUser, @"(补偿|compensat|R2R|下发)", RegexOptions.IgnoreCase))
            return Task.FromResult(new LlmDecision(true, "r2r_comp", "{}", null));
        if (Regex.IsMatch(lastUser, @"(优化|optimize|self-calibration|MRO)", RegexOptions.IgnoreCase))
            return Task.FromResult(new LlmDecision(true, "recipe_optimize", "{}", null));
        if (Regex.IsMatch(lastUser, @"(趋势|trend|近.*周|近.*月)", RegexOptions.IgnoreCase))
            return Task.FromResult(new LlmDecision(true, "tool_trend", "{}", null));
        if (Regex.IsMatch(lastUser, @"(光源|硬件|寿命|衰减|PM)", RegexOptions.IgnoreCase))
            return Task.FromResult(new LlmDecision(true, "hardware_health", "{}", null));
        if (Regex.IsMatch(lastUser, @"(经验|反馈|习得|learn)", RegexOptions.IgnoreCase))
            return Task.FromResult(new LlmDecision(true, "experience_learn", "{}", null));

        // 意图未匹配 -> 引导提示（FR-SYS-001 负例）
        var known = new[] { "统计 LOT", "为什么 fail", "分析处方", "机台性能", "诊断报错", "加载 Foup/slot" };
        if (lastUser.Length > 0 && !Regex.IsMatch(lastUser, @"你好|介绍|hello|hi", RegexOptions.IgnoreCase))
        {
            return Task.FromResult(new LlmDecision(false, null, null,
                $"未识别到明确意图。你可以试试：\n- {string.Join("\n- ", known)}"));
        }

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
