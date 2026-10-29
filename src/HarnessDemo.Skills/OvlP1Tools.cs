using HarnessDemo.Abstractions;

namespace HarnessDemo.Skills;

// ============================================================
// FR-PDA-003 性能分解 Budget
// ============================================================
public sealed class BudgetDecompTool : ITool
{
    public string Name => "budget_decomp";
    public string Description => "分解 Raw/W1F1/W3F3/CPE/Residual，识别最大贡献项。无入参。";
    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct) => Task.FromResult(
        "## 性能分解 Budget\n\n| 分量 | σ² | 占比 |\n|---|---|---|\n| Raw · Common | 8.0 | 45% |\n| W1F1 | 5.2 | 29% |\n| W3F3 | 2.1 | 12% |\n| CPE | 1.5 | 8% |\n| Residual | 1.0 | 6% |\n\n**最大贡献项**: Raw · Common（W1F1 主导），建议优先收敛机台级偏差。");
}

// ============================================================
// FR-PDA-004 归因分析（field-to-field）
// ============================================================
public sealed class AttributionTool : ITool
{
    public string Name => "attribution";
    public string Description => "field-to-field 残差分析，定位 overlay 偏大根因，给出 R² 证据。无入参。";
    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct) => Task.FromResult(
        "## 归因分析（边缘场 vs 其他场）\n\n| 区域 | X 3σ | Y 3σ | XY 综合 3σ |\n|---|---|---|---|\n| 边缘场 | 6.2 | 5.8 | 8.5 nm |\n| 其他场 | 3.8 | 3.5 | 5.2 nm |\n| 差值 | +2.4 | +2.3 | +3.3 nm |\n\n- R² ≈ 0.82（边缘场与 overlay 偏大强相关）\n- **疑似因素**: etch / CMP 引入的工艺误差，边缘 overlay 恶化严重。");
}

// ============================================================
// FR-PDA-005 决策建议
// ============================================================
public sealed class DecisionAdviceTool : ITool
{
    public string Name => "decision_advice";
    public string Description => "基于分析结果给出模型升级/采样/工艺改进建议。无入参。";
    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct) => Task.FromResult(
        "## 改进建议\n\n1. **模型升级**: 线性 → iHOPC（3 阶 15 参），预计 residual 降 15–20%\n2. **采样策略**: 边缘场加严采样（4 点 → 8 点）\n3. **工艺复核**: etch 功率窗口复核，CMP 均匀性确认");
}

// ============================================================
// FR-PDA-006 R2R 补偿下发
// ============================================================
public sealed class R2RCompTool : ITool
{
    public string Name => "r2r_comp";
    public string Description => "下发校正系数、更新阻尼、通知工艺复核，进入复测闭环。无入参。";
    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct) => Task.FromResult(
        "## R2R 补偿下发\n\n- ✅ 校正系数已下发：ΔX = +0.12 nm, ΔY = −0.08 nm\n- ✅ R2R 阻尼已更新：0.3 → 0.5\n- ✅ 已通知工艺复核\n- ⏳ 等待设备侧 OM 套刻量测复测回填（闭环中）");
}

// ============================================================
// FR-MGT-003 处方性能优化（知识库推荐）
// ============================================================
public sealed class RecipeOptimizeTool : ITool
{
    public string Name => "recipe_optimize";
    public string Description => "处方性能不达标时，按历史成功率推荐优化方案。入参 {current3sigma, target3sigma}。";
    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct) => Task.FromResult(
        "## 优化方案推荐（实测 3σ=0.41 nm，目标 0.25 nm）\n\n| # | 优化动作 | 历史匹配 | 成功率 | 预期 3σ |\n|---|---|---|---|---|\n| ① | 开启 Self-Calibration | 23 例 | 87% | 0.41 → 0.18 nm |\n| ② | 自动跑 MRO | 31 例 | 78% | 0.41 → 0.22 nm |\n| ③ | 调整 ANRA 参数 | 15 例 | 64% | 0.41 → 0.27 nm |\n\n[REQUIRES_CONFIRM] 请选择应用哪一方案，或介入调整参数。");
}

// ============================================================
// FR-TMA-002 机台性能趋势
// ============================================================
public sealed class ToolTrendTool : ITool
{
    public string Name => "tool_trend";
    public string Description => "近 N 周机台性能趋势，对比 spec 判定逼近程度。无入参。";
    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct) => Task.FromResult(
        "## 机台性能趋势（近 8 周重复性 3σ）\n\n| 周 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |\n|---|---|---|---|---|---|---|---|---|\n| 3σ (nm) | 0.18 | 0.19 | 0.20 | 0.20 | 0.21 | 0.22 | 0.23 | 0.24 |\n\n- Spec = 0.25 nm，末周 0.24 已逼近上限\n- 建议: 自查 TIS/calibration，安排 PM");
}

// ============================================================
// FR-TMA-003 硬件状态分析（光源寿命）
// ============================================================
public sealed class HardwareHealthTool : ITool
{
    private readonly OvlDataStore _db;
    public HardwareHealthTool(OvlDataStore db) => _db = db;
    public string Name => "hardware_health";
    public string Description => "分析硬件（量测光源）衰减趋势，预估剩余寿命与更换时点。无入参。";
    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct)
    {
        var monthsToThreshold = (_db.LightSourceCurrentBrightness - _db.LightSourceReplaceThreshold) / _db.LightSourceDecayPerMonth;
        return Task.FromResult(
            $"## 量测光源健康状态\n\n" +
            $"- 当前亮度: {_db.LightSourceCurrentBrightness:P0}（第 9 月）\n" +
            $"- 衰减速率: {_db.LightSourceDecayPerMonth:P1}/月（R² ≈ {_db.LightSourceR2}）\n" +
            $"- 更换阈值: {_db.LightSourceReplaceThreshold:P0}\n" +
            $"- **预计剩余寿命**: 约 {Math.Round(monthsToThreshold, 1)} 个月\n" +
            $"- 建议: 提前排程 PM 备件，约第 11 月更换光源");
    }
}

// ============================================================
// FR-TDI-003 经验反馈学习
// ============================================================
public sealed class ExperienceLearnTool : ITool
{
    public string Name => "experience_learn";
    public string Description => "用户反馈真实根因，写入诊断记忆，后续同类报错优先匹配。入参 {rootCause}。";
    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct) => Task.FromResult(
        "## 经验已习得\n\n- 已记录: 相机 PEG 采图失败\n- 来源: 用户经验\n- 后续同类「精对准失败」报错将优先排查相机 PEG 采图链路。");
}
