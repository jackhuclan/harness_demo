using HarnessDemo.Abstractions;

namespace HarnessDemo.Session;

/// <summary>
/// 路由调度（对应架构图「会话与路由层 / Router」）。
/// 职责：意图识别、Agent 选择、负载均衡。Demo 中只有一个 main-agent。
/// </summary>
public sealed class Router
{
    public string Route(string userInput)
    {
        // 生产环境：这里可以做关键词分类 / 向量检索最相似 Agent / 多 Agent 负载均衡
        var agent = "main-agent";
        Console.WriteLine($"[Session/Router] 意图识别完成 -> 路由至 {agent}");
        return agent;
    }
}
