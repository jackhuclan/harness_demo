using System.Text.Json;
using HarnessDemo.Abstractions;

namespace HarnessDemo.Skills;

// ============================================================
// FR-MGT-004 机台调度与硅片加载
// ============================================================
public sealed class WaferLoadTool : ITool
{
    public string Name => "wafer_load";
    public string Description => "根据 Foup/slot 加载硅片，返回 Wafer ID 与规格。入参 {foup, slot}。";

    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argsJson ?? "{}") ?? new();
        var foup = args.GetValueOrDefault("foup").ValueKind == JsonValueKind.String ? args["foup"].GetString() : "Foup2";
        var slot = args.GetValueOrDefault("slot").ValueKind == JsonValueKind.Number ? args["slot"].GetInt32() : 5;

        // 模拟失败：slot 0 或不存在
        if (slot < 1 || slot > 25)
            return Task.FromResult($"[FAIL] {foup}/slot{slot} 加载失败：slot 不存在，请检查 Foup 载片台。");

        var waferId = $"W-{foup.Replace("Foup", "").PadLeft(2, '0')}{slot:00}-001";
        return Task.FromResult(
            $"[LOAD OK] 已加载 {foup}/slot{slot}\n" +
            $"  Wafer ID: {waferId}\n" +
            $"  规格: 300mm bare Si\n" +
            $"  状态: 真空吸附 OK / 对心 OK，可进入量测或建方流程。");
    }
}

// ============================================================
// FR-MGT-002 处方分析
// ============================================================
public sealed class RecipeAnalyzeTool : ITool
{
    private readonly OvlDataStore _db;
    public RecipeAnalyzeTool(OvlDataStore db) => _db = db;
    public string Name => "recipe_analyze";
    public string Description => "分析指定处方：基本信息/运行统计/失败归类/优化建议。入参 {recipeName}。";

    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct)
    {
        var info = _db.RecipeInfo;
        var run = _db.RecipeRunStats;
        var fail = _db.RecipeFailBreakdown;
        return Task.FromResult(
            $"## 🟢 OVL Agent · 已自动路由：处方分析\n\n" +
            $"识别意图：分析处方 **ProductA_M1M2_Layer** 的运行情况。正在拉取该处方的量测记录与配置…\n\n" +
            $"### 📋 处方基本信息概况\n\n" +
            $"| | |\n|---|---|\n" +
            $"| 产品/层 | ProductA · M1M2 · Layer（套刻层） |\n" +
            $"| 标记类型 | BIB 标记 ｜ OVL Model: linear |\n" +
            $"| TIS 模式 | All Wafer ｜ Auto Retrain Test: 关闭 |\n" +
            $"| 所属机台 | OM800 ｜ 创建于: 2026-03-12 |\n\n" +
            $"### 📊 处方运行情况（近 30 天）\n\n" +
            $"- 已量测：**42 Lot ｜ 980 Wafer**\n" +
            $"- 测点成功率：**99.9%**（失败点 100 / 总 98,000）\n" +
            $"- 硅片成功率：**99.8%**（失败 2 / 成功 978）\n\n" +
            $"### 🔍 测点失败原因归类统计\n\n" +
            $"| 原因 | 次数 | 占比 |\n|---|---|---|\n" +
            $"| OVL 超限 | 38 | 38.0% |\n" +
            $"| TIS 超限 | 20 | 20.0% |\n" +
            $"| Qmerit 超限 | 18 | 18.0% |\n" +
            $"| 聚焦失败 | 14 | 14.0% |\n" +
            $"| 对准失败 | 10 | 10.0% |\n\n" +
            $"### 💡 优化建议（Agent 推荐）\n\n" +
            $"① Lot 间不同 wafer TIS 差异普遍较小（约 0.5 nm）→ 建议将 **TIS 模式改为 First Wafer**，可省去每片 TIS 量测，提升产率。\n\n" +
            $"② 不同 Lot 间亮度及 Focus offset 差异较大 → 建议开启 **Auto Retrain Test**，自动重训以应对跨 Lot 漂移。");
    }
}

// ============================================================
// FR-PDA-001 数据统计
// ============================================================
public sealed class OvlStatisticsTool : ITool
{
    private readonly OvlDataStore _db;
    public OvlStatisticsTool(OvlDataStore db) => _db = db;
    public string Name => "ovl_statistics";
    public string Description => "统计 LOT 的 OVL 指标：X/Y 3σ、片内最大 3σ、TIS 3σ，超规告警。入参 {lot}。";

    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct)
    {
        var xs = _db.WaferX3Sigma();
        var ys = _db.WaferY3Sigma();
        var maxX = xs.Max();
        var maxXWafer = Array.IndexOf(xs, maxX) + 1;
        var avgX = Math.Round(xs.Average(), 2);
        var avgY = Math.Round(ys.Average(), 2);
        var spec = 5.0; // nm 示例 spec
        var warn = maxX > spec ? " ⚠ 超 Spec" : "";
        return Task.FromResult(
            $"## LOT-2207 统计\n\n" +
            $"- 采样规模: {_db.Lot2207WaferCount} 片 × {_db.FieldsPerWafer} 场 × {_db.PointsPerField} 点\n" +
            $"- X 3σ 均值: {avgX} nm\n" +
            $"- Y 3σ 均值: {avgY} nm\n" +
            $"- 片内最大 X 3σ: Wafer {maxXWafer} = {maxX} nm{warn}\n" +
            $"- TIS 3σ: {_db.Tis3Sigma} nm");
    }
}

// ============================================================
// FR-PDA-002 异常分析 / 根因研判
// ============================================================
public sealed class OvlAnomalyTool : ITool
{
    private readonly OvlDataStore _db;
    public OvlAnomalyTool(OvlDataStore db) => _db = db;
    public string Name => "ovl_anomaly";
    public string Description => "失败范围定位 + 数据/图像/log 综合根因研判。无入参。";

    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct)
    {
        var (total, fail, mark, ovl) = _db.Wafer9FailStats;
        var rate = Math.Round(100.0 * fail / total, 0);
        return Task.FromResult(
            $"## 异常分析\n\n" +
            $"### 失败范围\n" +
            $"- 整批 25 片中，Wafer 9 fail 率 {rate}%，其余 24 片 < 1% → 定位为单片问题。\n\n" +
            $"### 失败测点归类（Wafer 9）\n" +
            $"- 总测点: {total}，fail: {fail}\n" +
            $"- 标记识别失败: {mark}\n" +
            $"- OVL 超限: {ovl}\n\n" +
            $"### 综合研判（图像 + 报表 + log）\n" +
            $"- {fail} 个失败测点中 {Math.Round(mark * 1.23, 0)} 个（~90%）对应图像中标记脏污/破损区域\n" +
            $"- 设备 log 无光学/机械异常\n" +
            $"- **根因**: Wafer 9 来料标记脏污破损（非机台/工艺系统性问题）\n\n" +
            $"### 处置建议\n" +
            $"- 清洁 / re-mark 后重测该片，或标记该片异常并剔除\n" +
            $"- 复测由 OM 套刻量测设备侧执行");
    }
}

// ============================================================
// FR-TMA-001 机台性能检测
// ============================================================
public sealed class ToolPerformanceTool : ITool
{
    private readonly OvlDataStore _db;
    public ToolPerformanceTool(OvlDataStore db) => _db = db;
    public string Name => "tool_performance";
    public string Description => "运行 Tool Performance 处方，统计重复性/TIS/MAM 并判定在控；超标时排序候选原因。无入参。";

    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct)
    {
        var (rep, tisMean, tis3, mam) = _db.ToolPerformanceGood;
        var spec = _db.ToolRepeatabilitySpec;
        var inControl = rep <= spec;
        var lines = new System.Text.StringBuilder();
        lines.AppendLine("## 机台性能检测（Tool Performance）\n");
        lines.AppendLine($"| 指标 | 数值 | 判定 |");
        lines.AppendLine($"|---|---|---|");
        lines.AppendLine($"| OVL 重复性 (3σ) | {rep} nm | {(inControl ? "在控" : "⚠ 超 Spec")} (spec {spec}) |");
        lines.AppendLine($"| TIS Mean | {tisMean} nm | 在控 |");
        lines.AppendLine($"| TIS 3σ | {tis3} nm | 在控 |");
        lines.AppendLine($"| MAM | {mam} s | 在控 |");

        if (!inControl)
        {
            lines.AppendLine("\n### 超标候选原因（按历史命中率排序）\n");
            lines.AppendLine("| 原因 | 历史匹配 | 成功率 | 预期 3σ |");
            lines.AppendLine("|---|---|---|---|");
            foreach (var c in _db.OverRepeatabilityCauses)
                lines.AppendLine($"| {c.cause} | {c.matches} 例 | {c.success:P0} | 0.87 → {c.after3sigma} nm |");
        }
        return Task.FromResult(lines.ToString());
    }
}

// ============================================================
// FR-TDI-001 报错诊断
// ============================================================
public sealed class OvlDiagnosisTool : ITool
{
    private readonly OvlDataStore _db;
    public OvlDiagnosisTool(OvlDataStore db) => _db = db;
    public string Name => "ovl_diagnose";
    public string Description => "识别报错类型并调取 log 定位根因。入参 {error}。";

    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argsJson ?? "{}") ?? new();
        var error = args.GetValueOrDefault("error").ValueKind == JsonValueKind.String ? args["error"].GetString() : "粗聚焦失败";

        if (error!.Contains("聚焦"))
        {
            var d = _db.FocusFailDiagnosis;
            return Task.FromResult(
                $"## 诊断结果\n\n" +
                $"- 报错: {d.error}\n" +
                $"- 根因: {d.rootCause}\n" +
                $"- 处置建议: {d.recommendation}\n\n" +
                $"[REQUIRES_CONFIRM] 关键处置执行前需用户确认。是否执行参数修改（Focus Travel 50→80μm）？");
        }
        return Task.FromResult(
            $"## 诊断结果\n\n- 报错: {error}\n- 未匹配到已知根因，请提供运行 log 或描述现场现象。");
    }
}
