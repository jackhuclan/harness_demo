namespace HarnessDemo.Abstractions;

/// <summary>安全层审计结果。</summary>
public sealed record AuditResult(bool Passed, string? Reason = null);

/// <summary>安全层：内容安全 / 敏感词检测（入口前置闸门）。</summary>
public interface IContentAuditor
{
    Task<AuditResult> AuditAsync(string content, CancellationToken ct);
}
