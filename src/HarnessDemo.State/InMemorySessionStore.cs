using System.Text.Json;
using HarnessDemo.Abstractions;

namespace HarnessDemo.State;

/// <summary>状态层：会话存储（内存 + 磁盘持久化到 sessions/ 目录）。</summary>
public sealed class InMemorySessionStore : ISessionStore
{
    private readonly Dictionary<string, AgentSession> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private readonly string _dir;

    public InMemorySessionStore(string? dir = null)
    {
        _dir = dir ?? Path.Combine(AppContext.BaseDirectory, "sessions");
        Directory.CreateDirectory(_dir);
        LoadAll();
    }

    private void LoadAll()
    {
        foreach (var f in Directory.GetFiles(_dir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(f);
                var snapshot = JsonSerializer.Deserialize<SessionSnapshot>(json);
                if (snapshot == null) continue;
                var s = new AgentSession { Id = snapshot.Id, CreatedAt = snapshot.CreatedAt, UpdatedAt = snapshot.UpdatedAt };
                foreach (var m in snapshot.Messages)
                    s.Messages.Add(new ChatMessage(m.Role, m.Content, m.ToolName));
                _sessions[s.Id] = s;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[State/SessionStore] 加载 {f} 失败: {ex.Message}");
            }
        }
        Console.WriteLine($"[State/SessionStore] 从磁盘加载 {_sessions.Count} 个会话");
    }

    private void SaveToDisk(AgentSession session)
    {
        try
        {
            var snap = new SessionSnapshot
            {
                Id = session.Id,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                Messages = session.Messages.Select(m => new MessageSnap
                {
                    Role = m.Role, Content = m.Content, ToolName = m.ToolName
                }).ToList()
            };
            var path = Path.Combine(_dir, $"{session.Id}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(snap, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[State/SessionStore] 写盘失败: {ex.Message}");
        }
    }

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
            SaveToDisk(session);
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

    // --- 序列化快照 ---
    private sealed class SessionSnapshot
    {
        public string Id { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public List<MessageSnap> Messages { get; set; } = new();
    }
    private sealed class MessageSnap
    {
        public Role Role { get; set; }
        public string Content { get; set; } = "";
        public string? ToolName { get; set; }
    }
}
