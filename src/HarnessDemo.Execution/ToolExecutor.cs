using System.Diagnostics;
using HarnessDemo.Abstractions;

namespace HarnessDemo.Execution;

/// <summary>
/// 执行层：工具执行器（对应架构图「执行层 / Tool Executor」）。
/// 职责：注册表、超时控制、异常捕获、失败重试一次、结果收集。
/// </summary>
public sealed class ToolExecutor : IToolExecutor
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ITool> RegisteredTools => _tools.Values.ToList();

    public void Register(ITool tool) => _tools[tool.Name] = tool;

    public async Task<ToolResult> ExecuteAsync(string toolName, string argsJson, CancellationToken ct)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
            return new ToolResult(false, "", $"工具 '{toolName}' 未注册");

        var sw = Stopwatch.StartNew();
        try
        {
            // 超时保护（沙箱资源限制的简化版）
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            var output = await tool.ExecuteAsync(argsJson, timeout.Token);
            sw.Stop();
            Console.WriteLine($"[Execution/ToolExecutor] {toolName} 成功，耗时 {sw.ElapsedMilliseconds} ms");
            return new ToolResult(true, output);
        }
        catch (Exception ex)
        {
            // 异常处理 + 重试一次
            Console.WriteLine($"[Execution/ToolExecutor] {toolName} 首次失败（{ex.Message}），重试一次");
            try
            {
                var output = await tool.ExecuteAsync(argsJson, ct);
                sw.Stop();
                Console.WriteLine($"[Execution/ToolExecutor] {toolName} 重试成功，耗时 {sw.ElapsedMilliseconds} ms");
                return new ToolResult(true, output);
            }
            catch (Exception ex2)
            {
                sw.Stop();
                Console.WriteLine($"[Execution/ToolExecutor] {toolName} 重试仍失败：{ex2.Message}");
                return new ToolResult(false, "", ex2.Message);
            }
        }
    }
}
