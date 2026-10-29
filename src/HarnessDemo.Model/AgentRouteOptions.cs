namespace HarnessDemo.Model;

/// <summary>Agent 路由配置：第一阶段（用户意图 -> Agent）+ 第二阶段（Agent -> 工具）。</summary>
public sealed class AgentRouteOptions
{
    public const string SectionName = "AgentRouting";

    /// <summary>第一阶段：关键词正则 -> Agent 名</summary>
    public List<AgentRouteEntry> AgentRoutes { get; set; } = new();

    /// <summary>第二阶段：Agent 名 -> 该 Agent 内的工具路由列表</summary>
    public Dictionary<string, List<ToolRouteEntry>> ToolRoutes { get; set; } = new();
}

public sealed class AgentRouteEntry
{
    public string Pattern { get; set; } = "";
    public string Agent { get; set; } = "";
}

public sealed class ToolRouteEntry
{
    public string Pattern { get; set; } = "";
    public string Tool { get; set; } = "";
    public string Args { get; set; } = "{}";
}
