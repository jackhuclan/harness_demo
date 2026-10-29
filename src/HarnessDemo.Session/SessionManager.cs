using HarnessDemo.Abstractions;
using HarnessDemo.AgentCore;

namespace HarnessDemo.Session;

/// <summary>
/// 会话管理器（对应架构图「会话与路由层 / Session Manager」）。
/// 对外门面：入口层只依赖它；内部串起 Router -> Agent Core。
/// </summary>
public sealed class SessionManager
{
    private readonly ISessionStore _sessions;
    private readonly Agent _agent;
    private readonly Router _router;

    public SessionManager(ISessionStore sessions, Agent agent, Router router)
    {
        _sessions = sessions;
        _agent = agent;
        _router = router;
    }

    public async Task<string> AskAsync(string sessionId, string userInput, CancellationToken ct = default)
    {
        Console.WriteLine($"\n[Session/SessionManager] 收到请求，会话 {sessionId}");
        _router.Route(userInput);
        return await _agent.RunAsync(sessionId, userInput, ct);
    }
}
