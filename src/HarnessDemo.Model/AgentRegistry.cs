namespace HarnessDemo.Model;

public sealed record AgentDef(string Name, string Title, string Description, string SystemPrompt, string[] Tools);

/// <summary>多智能体注册表：每个 Agent 有自己的人设、系统提示词和工具子集。</summary>
public static class AgentRegistry
{
    public static readonly AgentDef[] Agents = new[]
    {
        new AgentDef("intro", "智能体介绍", "介绍整个系统的能力与使用方式",
            "你是 Agent Harness 的引导助手，向用户介绍系统能力和各智能体用途。",
            Array.Empty<string>()),

        new AgentDef("recipe", "处方管理", "Wafer 装载、处方分析、处方优化、建方流程",
            "我是 OVL Agent，处方管理。支持两类任务：① 处方建立及调试、② 处方分析。",
            new[] { "wafer_load", "recipe_analyze", "recipe_optimize", "recipe_create", "custom_skill_learn" }),

        new AgentDef("pda", "生产数据分析", "OVL 统计 / 异常 / 分解 / 归因 / 决策 / 补偿 / 多技能串联",
            "我可直接统计生产片 OVL 情况（数据统计 / 异常分析 / 分解 / 归因 / 决策 / 补偿 / 多技能串联）。",
            new[] { "ovl_statistics", "ovl_anomaly", "budget_decomp", "attribution", "decision_advice", "r2r_comp", "ovl_chain" }),

        new AgentDef("tma", "机台监测分析", "机台性能、周趋势、硬件健康（光源寿命）",
            "你是 OVL 机台监测专家，负责机台性能统计、趋势分析和硬件健康评估。",
            new[] { "tool_performance", "tool_trend", "hardware_health" }),

        new AgentDef("tdi", "机台诊断修复", "报错诊断、处置确认执行、经验学习",
            "你是 OVL 机台诊断修复专家，负责报错定位、处置执行和经验沉淀。",
            new[] { "ovl_diagnose", "disposition_confirm", "experience_learn" }),
    };

    public static AgentDef Get(string name) =>
        Agents.FirstOrDefault(a => a.Name == name) ?? Agents[0];
}
