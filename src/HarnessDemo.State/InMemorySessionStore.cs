using HarnessDemo.Abstractions;

namespace HarnessDemo.State;

/// <summary>状态层：会话存储（内存实现，生产可换 Redis / 数据库）。</summary>
public sealed class InMemorySessionStore : ISessionStore
{
    private readonly Dictionary<string, AgentSession> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public Task<AgentSession> GetOrCreateAsync(string sessionId, CancellationToken ct)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var s))
            {
                s = new AgentSession { Id = sessionId };
                _sessions[sessionId] = s;
                Console.WriteLine($"[State/SessionStore] 创建新会话 {sessionId}");
            }
            return Task.FromResult(s);
        }
    }

    public Task SaveAsync(AgentSession session, CancellationToken ct)
    {
        lock (_lock)
        {
            session.UpdatedAt = DateTimeOffset.Now;
            _sessions[session.Id] = session;
            Console.WriteLine($"[State/SessionStore] 持久化会话 {session.Id}（{session.Messages.Count} 条消息）");
        }
        return Task.CompletedTask;
    }

    public IReadOnlyList<AgentSession> ListAll()
    {
        lock (_lock)
        {
            return _sessions.Values.OrderByDescending(s => s.UpdatedAt).ToList();
        }
    }
}
