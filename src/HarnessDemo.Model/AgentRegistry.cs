namespace HarnessDemo.Model;

public sealed record AgentDef(string Name, string Title, string Description, string SystemPrompt, string[] Tools);

/// <summary>多智能体注册表：每个 Agent 有自己的人设、系统提示词和工具子集。</summary>
public static class AgentRegistry
{
    public static readonly AgentDef[] Agents = new[]
    {
        new AgentDef("intro", "智能体介绍", "介绍整个系统的能力与使用方式",
            "我是 OVL Agent 介绍助手。当你没有明确目的时，我会为你介绍系统有哪些技能、功能，以及如何开始使用。",
            Array.Empty<string>()),

        new AgentDef("recipe", "处方管理", "Wafer 装载、处方分析、处方优化、建方流程",
            "我是 OVL Agent，处方管理。支持两类任务：① 处方建立及调试、② 处方分析。",
            new[] { "wafer_load", "recipe_analyze", "recipe_optimize", "recipe_create", "custom_skill_learn" }),

        new AgentDef("pda", "生产数据分析", "OVL 统计 / 异常 / 分解 / 归因 / 决策 / 补偿 / 多技能串联",
            "我可直接统计生产片 OVL 情况（数据统计 / 异常分析 / 分解 / 归因 / 决策 / 补偿 / 多技能串联）。",
            new[] { "ovl_statistics", "ovl_anomaly", "budget_decomp", "attribution", "decision_advice", "r2r_comp", "ovl_chain" }),

        new AgentDef("tma", "机台监测分析", "机台性能检测、近两月趋势、光源硬件寿命预估",
            "我是机台监测分析智能体。可统计机台本身的性能（重复性、TIS 误差等），并对机台硬件状态做分析与寿命预估。",
            new[] { "tool_performance", "tool_trend", "hardware_health" }),

        new AgentDef("tdi", "机台诊断修复", "精对准/粗聚焦报错根因诊断、处置执行、经验学习",
            "我是机台诊断修复智能体。可识别机台报错根因、自动处置并支持用户介入验证。当用户点击示例或输入指令（如「看下当前机台为什么报错」）触发。",
            new[] { "ovl_diagnose", "disposition_confirm", "experience_learn" }),
    };

    public static AgentDef Get(string name) =>
        Agents.FirstOrDefault(a => a.Name == name) ?? Agents[0];
}
