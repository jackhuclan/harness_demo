using HarnessDemo.Skills;
using HarnessDemo.Abstractions;
using Xunit;

namespace HarnessDemo.Tests;

// 对齐 PRD 第三部分 TC 编号，覆盖 P0/P1 核心工具
public class OvlToolsTests
{
    private readonly OvlDataStore _db = new();

    [Fact] // TC-MGT-004-01
    public async Task WaferLoad_Success_ReturnsWaferId()
    {
        var tool = new WaferLoadTool();
        var r = await tool.ExecuteAsync("{\"foup\":\"Foup2\",\"slot\":5}", default);
        Assert.Contains("[LOAD OK]", r);
        Assert.Contains("Wafer ID", r);
    }

    [Fact] // TC-MGT-004-02
    public async Task WaferLoad_InvalidSlot_ReturnsFail()
    {
        var tool = new WaferLoadTool();
        var r = await tool.ExecuteAsync("{\"foup\":\"Foup2\",\"slot\":99}", default);
        Assert.Contains("[FAIL]", r);
    }

    [Fact] // TC-MGT-002-01
    public async Task RecipeAnalyze_ReturnsFourSections()
    {
        var tool = new RecipeAnalyzeTool(_db);
        var r = await tool.ExecuteAsync("{}", default);
        Assert.Contains("基本信息", r);
        Assert.Contains("运行统计", r);
        Assert.Contains("失败归类", r);
        Assert.Contains("优化建议", r);
    }

    [Fact] // TC-PDA-001-01/02
    public async Task Statistics_Outputs3sigmaAndWarns()
    {
        var tool = new OvlStatisticsTool(_db);
        var r = await tool.ExecuteAsync("{}", default);
        Assert.Contains("X 3σ", r);
        Assert.Contains("TIS 3σ", r);
        Assert.Contains("⚠", r); // Wafer 9 超规
    }

    [Fact] // TC-PDA-002-01/02
    public async Task Anomaly_LocatesWafer9RootCause()
    {
        var tool = new OvlAnomalyTool(_db);
        var r = await tool.ExecuteAsync("{}", default);
        Assert.Contains("Wafer 9", r);
        Assert.Contains("标记脏污", r);
    }

    [Fact] // TC-TMA-001-01
    public async Task ToolPerformance_ReportsInControl()
    {
        var tool = new ToolPerformanceTool(_db);
        var r = await tool.ExecuteAsync("{}", default);
        Assert.Contains("重复性", r);
        Assert.Contains("在控", r);
    }

    [Fact] // TC-TDI-001-01 + SYS-003 介入
    public async Task Diagnose_FocusFail_RequiresConfirmation()
    {
        var tool = new OvlDiagnosisTool(_db);
        var r = await tool.ExecuteAsync("{\"error\":\"粗聚焦失败\"}", default);
        Assert.Contains("聚焦行程不足", r);
        Assert.Contains("[REQUIRES_CONFIRM]", r);
    }

    [Fact] // TC-PDA-003-01
    public async Task Budget_IdentifiesMaxContributor()
    {
        var tool = new BudgetDecompTool();
        var r = await tool.ExecuteAsync("{}", default);
        Assert.Contains("Raw · Common", r);
    }

    [Fact] // TC-PDA-004-01
    public async Task Attribution_ReportsEdgeFieldR2()
    {
        var tool = new AttributionTool();
        var r = await tool.ExecuteAsync("{}", default);
        Assert.Contains("边缘场", r);
        Assert.Contains("R²", r);
    }

    [Fact] // TC-MGT-003-01
    public async Task RecipeOptimize_RanksBySuccessRate()
    {
        var tool = new RecipeOptimizeTool();
        var r = await tool.ExecuteAsync("{}", default);
        Assert.Contains("Self-Calibration", r);
        Assert.Contains("87%", r);
    }

    [Fact] // TC-TMA-003-01
    public async Task HardwareHealth_PredictsRemainingLife()
    {
        var tool = new HardwareHealthTool(_db);
        var r = await tool.ExecuteAsync("{}", default);
        Assert.Contains("剩余寿命", r);
        Assert.Contains("R²", r);
    }

    [Fact] // TC-TDI-003-01
    public async Task ExperienceLearn_ConfirmsMemory()
    {
        var tool = new ExperienceLearnTool();
        var r = await tool.ExecuteAsync("{}", default);
        Assert.Contains("已习得", r);
    }
}
