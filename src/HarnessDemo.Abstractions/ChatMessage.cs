namespace HarnessDemo.Abstractions;

/// <summary>对话消息角色（对应架构图 Context 上下文管理）。</summary>
public enum Role
{
    System,
    User,
    Assistant,
    Tool
}

/// <summary>一条上下文消息。</summary>
public sealed record ChatMessage(Role Role, string Content, string? ToolName = null)
{
    public override string ToString() => $"[{Role}] {(ToolName is null ? Content : $"{ToolName}: {Content}")}";
}
