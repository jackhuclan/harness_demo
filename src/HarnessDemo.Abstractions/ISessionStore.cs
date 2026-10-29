namespace HarnessDemo.Abstractions;

/// <summary>会话实体（状态层 Session Store）。</summary>
public sealed class AgentSession
{
    public required string Id { get; init; }
    public List<ChatMessage> Messages { get; } = new();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
}

/// <summary>状态层：会话存储（会话创建/维护、历史持久化）。</summary>
public interface ISessionStore
{
    Task<AgentSession> GetOrCreateAsync(string sessionId, CancellationToken ct);
    Task SaveAsync(AgentSession session, CancellationToken ct);
    IReadOnlyList<AgentSession> ListAll();
}
