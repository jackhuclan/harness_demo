using HarnessDemo.Abstractions;

namespace HarnessDemo.Skills;

// ============================================================
// FR-PDA-007 多技能串联：单条指令自动跑 统计→异常→分解→归因→建议
// ============================================================
public sealed class MultiSkillChainTool : ITool
{
    private readonly OvlStatisticsTool _stats;
    private readonly OvlAnomalyTool _anomaly;
    private readonly BudgetDecompTool _budget;
    private readonly AttributionTool _attr;
    private readonly DecisionAdviceTool _advice;

    public MultiSkillChainTool(
        OvlStatisticsTool stats, OvlAnomalyTool anomaly,
        BudgetDecompTool budget, AttributionTool attr, DecisionAdviceTool advice)
    {
        _stats = stats; _anomaly = anomaly; _budget = budget; _attr = attr; _advice = advice;
    }

    public string Name => "ovl_chain";
    public string Description => "多技能串联：自动跑统计→异常→分解→归因→建议，输出完整链路结论。";

    public async Task<string> ExecuteAsync(string argsJson, CancellationToken ct)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# 多技能串联分析链路\n");
        sb.AppendLine(await _stats.ExecuteAsync("{}", ct));
        sb.AppendLine();
        sb.AppendLine(await _anomaly.ExecuteAsync("{}", ct));
        sb.AppendLine();
        sb.AppendLine(await _budget.ExecuteAsync("{}", ct));
        sb.AppendLine();
        sb.AppendLine(await _attr.ExecuteAsync("{}", ct));
        sb.AppendLine();
        sb.AppendLine(await _advice.ExecuteAsync("{}", ct));
        return sb.ToString();
    }
}

// ============================================================
// FR-TDI-002 介入确认后执行处置 + 复测
// ============================================================
public sealed class DispositionConfirmTool : ITool
{
    public string Name => "disposition_confirm";
    public string Description => "用户确认后执行处置参数修改并复测。入参 {confirmed:true}。";

    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct) => Task.FromResult(
        "## 处置执行完成\n\n" +
        "- ✅ Focus Travel 已修改：50μm → 80μm\n" +
        "- ✅ 重新量测通过，重复性恢复 0.22 nm（spec 0.25）\n" +
        "- ✅ 机台恢复正常运行");
}

// ============================================================
// FR-MGT-001 处方建立及调试（13 步流程，关键步骤暂停）
// ============================================================
public sealed class RecipeCreateTool : ITool
{
    public string Name => "recipe_create";
    public string Description => "引导 13 步建方流程，关键步骤自动暂停等待用户确认。入参 {foup, slot}。";

    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct) => Task.FromResult(
        "## 处方建立及调试（13 步流程）\n\n" +
        "| 步骤 | 环节 | 关键/介入 |\n" +
        "|---|---|---|\n" +
        "| 1 | 输入 WaferMap 参数 | |\n" +
        "| 2 | 对准建模（coarse→fine→LLC） | ⏸ 关键·待确认 |\n" +
        "| 3 | 创建 Test（设置 Target） | ⏸ 关键·待确认 |\n" +
        "| 4 | 训练 Test | ⏸ 关键·待确认 |\n" +
        "| 5 | 设置物理参数（SG/DG/Focus/WLI/Model） | ⏸ 关键·待确认 |\n" +
        "| 6 | 自动选取量测 Field | 可介入 |\n" +
        "| 7 | 自动设置 Data culling | 可介入 |\n" +
        "| 8 | 自动设置 TIS Optimization | 可介入 |\n" +
        "| 9 | 处方创建完毕 | |\n" +
        "| 10 | 处方性能确认 | ⏸ 关键 |\n" +
        "| 11 | 调用量测流程执行性能测试 | 可介入 |\n" +
        "| 12 | 自动分析量测性能 | |\n" +
        "| 13 | 性能判断与收尾（达标保存/不达标优化） | ⏸ 关键 |\n\n" +
        "**性能指标（示例）**：重复性 3σ 0.42 nm / TIS 0.18 nm / 模型残差 0.31 nm（均 Pass，spec < 0.6 nm）\n\n" +
        "[REQUIRES_CONFIRM] 步骤 2 对准建模已暂停，请确认或介入参数。");
}

// ============================================================
// FR-CSK-001/002 自定义技能习得与执行
// ============================================================
public sealed class CustomSkillLearnTool : ITool
{
    private static readonly List<string> _learnedSkills = new();

    public string Name => "custom_skill_learn";
    public string Description => "解析自然语言规则并习得为自定义技能。入参 {rule}。";

    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct)
    {
        var rule = argsJson ?? "动态采样规则：按来料滚动应用多种采样方案";
        if (!_learnedSkills.Contains(rule)) _learnedSkills.Add(rule);
        return Task.FromResult(
            $"## 自定义技能已习得\n\n" +
            $"- 规则内容：{rule}\n" +
            $"- 当前已习得技能数：{_learnedSkills.Count}\n" +
            $"- 后续匹配任务将自动按此规则执行。");
    }
}
