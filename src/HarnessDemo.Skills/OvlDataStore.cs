namespace HarnessDemo.Skills;

/// <summary>
/// OVL 演示数据仓库：seeded in-memory，数值与 PRD 示例一致。
/// 未来接真实设备报表时替换此实现即可。
/// </summary>
public sealed class OvlDataStore
{
    // --- LOT-2207 量测数据（PRD §3.3 / §4.4 示例）---
    public int Lot2207WaferCount => 25;
    public int FieldsPerWafer => 17;
    public int PointsPerField => 4;

    /// <summary>每片的 X 3σ（nm），Wafer 9 为 6.2（超规）。</summary>
    public double[] WaferX3Sigma() =>
        Enumerable.Range(1, 25).Select(w => w == 9 ? 6.2 : Math.Round(3.5 + Random.Shared.NextDouble(), 2)).ToArray();

    public double[] WaferY3Sigma() =>
        Enumerable.Range(1, 25).Select(w => w == 9 ? 5.8 : Math.Round(3.2 + Random.Shared.NextDouble(), 2)).ToArray();

    public double Tis3Sigma => 0.9;

    /// <summary>Wafer 9 失败测点：68 个测点 52 fail，其中标记识别 38、OVL 超限 14。</summary>
    public (int total, int fail, int markFail, int ovlFail) Wafer9FailStats => (68, 52, 38, 14);

    // --- ProductA_M1M2_Layer 处方（PRD §3.2.2）---
    public (string product, string layer, string mark, string ovlModel, string tisMode, string tool, string created)
        RecipeInfo => ("ProductA", "M1M2", "BK/BAR", "iHOPC(3rd)", "Auto", "OM800#1", "2026-08-15");

    public (int lots, int wafers, double pointRate, double waferRate) RecipeRunStats =>
        (42, 980, 99.9, 99.8);

    public (int ovl, int tis, int qmerit, int focus, int align) RecipeFailBreakdown =>
        (38, 20, 18, 14, 10);

    // --- Tool Performance（PRD §4.5 首次全在控；超标示例 0.87）---
    public (double repeatability, double tisMean, double tis3sigma, double mam) ToolPerformanceGood =>
        (0.21, 0.17, 0.24, 0.28);
    public double ToolRepeatabilitySpec => 0.25;

    public (string cause, int matches, double success, double after3sigma)[] OverRepeatabilityCauses => new[]
    {
        ("SFO 漂移", 38, 0.86, 0.21),
        ("Stage 稳定性", 23, 0.71, 0.52),
        ("光源稳定性超标", 19, 0.58, 0.61),
        ("光阑零位漂移", 12, 0.47, 0.66),
    };

    // --- 光源衰减（PRD §4.5）---
    public double LightSourceDecayPerMonth => 0.019; // 1.9%/月
    public double LightSourceR2 => 0.99;
    public double LightSourceCurrentBrightness => 0.85; // 第 9 月
    public double LightSourceReplaceThreshold => 0.80;

    // --- 诊断：粗聚焦失败（PRD §3.5）---
    public (string error, string rootCause, string recommendation) FocusFailDiagnosis =>
        ("粗聚焦失败", "聚焦行程不足（Focus Travel 50μm）", "建议 Focus Travel 50μm → 80μm");
}
