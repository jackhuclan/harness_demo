using System.Text.Json;
using HarnessDemo.Abstractions;

namespace HarnessDemo.Skills;

/// <summary>能力层工具：安全算术计算（对应架构图 Tools / 工具调用）。</summary>
public sealed class CalculatorTool : ITool
{
    public string Name => "calculator";
    public string Description => "安全计算算术表达式，例如 12*(3+4)/2";

    public Task<string> ExecuteAsync(string argsJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var expr = doc.RootElement.GetProperty("expression").GetString() ?? "";
        var result = SafeEval.Evaluate(expr);
        return Task.FromResult($"{expr.Trim()} = {result}");
    }
}
