using System.Text;
using HarnessDemo.Abstractions;
using HarnessDemo.Model;

namespace HarnessDemo.AgentCore;

/// <summary>
/// 上下文管理器。根据当前 Agent 渐进式加载系统提示词：
/// 基础平台说明 + Agent 人设 + 该 Agent 的技能列表 + 允许的工具列表。
/// </summary>
public sealed class ContextManager
{
    private const int MaxHistory = 10;
    private readonly AgentRegistry _registry;

    public ContextManager(AgentRegistry registry)
    {
        _registry = registry;
    }

    public IReadOnlyList<ChatMessage> Build(IReadOnlyList<ChatMessage> history, string? agentName = null)
    {
        var system = BuildSystemPrompt(agentName);
        var list = new List<ChatMessage> { new(Role.System, system) };

        var recent = history.Count <= MaxHistory
            ? history
            : history.TakeLast(MaxHistory).ToList();
        list.AddRange(recent);

        Console.WriteLine($"[AgentCore/Context] 组装上下文 {list.Count} 条（agent={agentName ?? "default"}，历史 {history.Count} 条）");
        return list;
    }

    private string BuildSystemPrompt(string? agentName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("你是运行在 Agent Harness 平台上的智能体。");

        if (string.IsNullOrEmpty(agentName))
        {
            sb.AppendLine("当前未指定具体 Agent，请引导用户选择或介绍系统能力。");
            return sb.ToString();
        }

        var agent = _registry.Get(agentName);
        sb.AppendLine();
        sb.AppendLine($"## 当前智能体：{agent.Title}");
        sb.AppendLine(agent.SystemPrompt);

        if (agent.Skills.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### 可用技能");
            foreach (var s in agent.Skills)
            {
                sb.AppendLine($"- **{s.Name}**：{s.Description}");
            }
        }

        if (agent.Tools.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### 可调用工具");
            sb.AppendLine(string.Join(", ", agent.Tools));
        }

        return sb.ToString();
    }
}
