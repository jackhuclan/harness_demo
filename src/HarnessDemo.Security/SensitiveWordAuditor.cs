using HarnessDemo.Abstractions;

namespace HarnessDemo.Security;

/// <summary>
/// 安全层：内容安全闸门（对应架构图右侧「内容安全 / 敏感词检测」）。
/// 在请求进入 Agent Core 之前执行，命中黑名单直接拦截。
/// </summary>
public sealed class SensitiveWordAuditor : IContentAuditor
{
    private static readonly string[] Blacklist = { "secret", "密码", "暴力", "攻击", "inject" };

    public Task<AuditResult> AuditAsync(string content, CancellationToken ct)
    {
        foreach (var word in Blacklist)
        {
            if (content.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[Security/Auditor] 命中敏感词「{word}」，请求被拦截");
                return Task.FromResult(new AuditResult(false, $"内容命中敏感词「{word}」，已按安全策略拦截"));
            }
        }
        Console.WriteLine("[Security/Auditor] 内容审计通过");
        return Task.FromResult(new AuditResult(true));
    }
}
