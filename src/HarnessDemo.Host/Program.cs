using DotNetEnv;
using HarnessDemo.Abstractions;
using HarnessDemo.AgentCore;
using HarnessDemo.Execution;
using HarnessDemo.Model;
using HarnessDemo.Security;
using HarnessDemo.Session;
using HarnessDemo.Skills;
using HarnessDemo.State;

// =====================================================================
// Agent Harness Demo —— 入口层：ASP.NET Core Web API
// 对应架构图「入口层 / API 开放接口」。
// 模型配置从 .env 读取（DotNetEnv）：USE_REAL_LLM=true 走真实大模型，false 走内置规则模拟。
// =====================================================================

// 加载仓库根目录 .env（向上逐级查找）
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

// --- 状态层 ---
builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();
builder.Services.AddSingleton<IMemoryStore, InMemoryMemoryStore>();
builder.Services.AddSingleton<ITaskStore, InMemoryTaskStore>();

// --- 模型层：按配置选择真实大模型 or 规则模拟 ---
var useRealLlm = Environment.GetEnvironmentVariable("USE_REAL_LLM")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
if (useRealLlm)
{
    var endpoint = Environment.GetEnvironmentVariable("LLM_ENDPOINT") ?? "http://localhost:3001/v1";
    var apiKey = Environment.GetEnvironmentVariable("LLM_API_KEY") ?? "";
    var model = Environment.GetEnvironmentVariable("LLM_MODEL") ?? "deepseek-v4.1-flash";
    var maxTokens = int.TryParse(Environment.GetEnvironmentVariable("LLM_MAX_TOKENS"), out var t) ? t : 4096;
    Console.WriteLine($"[Host] 使用真实大模型：{model} @ {endpoint}");
    builder.Services.AddSingleton<ILlmClient>(_ => new OpenAiLlmClient(endpoint, apiKey, model, maxTokens));
}
else
{
    Console.WriteLine("[Host] 使用内置规则模拟 LLM（离线模式）");
    builder.Services.AddSingleton<ILlmClient, RuleBasedLlmClient>();
}

// --- 安全层 / Agent Core ---
builder.Services.AddSingleton<IContentAuditor, SensitiveWordAuditor>();
builder.Services.AddSingleton<ContextManager>();
builder.Services.AddSingleton<Agent>();

// --- 执行层 + 能力层插件 ---
builder.Services.AddSingleton<ToolExecutor>();
builder.Services.AddSingleton<IToolExecutor>(sp => sp.GetRequiredService<ToolExecutor>());
builder.Services.AddSingleton<ITool, CalculatorTool>();
builder.Services.AddSingleton<ITool, TimeTool>();

// --- 会话与路由层 ---
builder.Services.AddSingleton<Router>();
builder.Services.AddSingleton<SessionManager>();

var app = builder.Build();

// 把 DI 注册的能力层插件登记进执行层注册表
var executor = app.Services.GetRequiredService<ToolExecutor>();
foreach (var tool in app.Services.GetServices<ITool>())
    executor.Register(tool);

// --- 入口层：HTTP API ---

// 服务发现 / 健康检查
app.MapGet("/", () => Results.Ok(new
{
    name = "Agent Harness API",
    endpoints = new[] { "POST /chat", "GET /tools", "GET /sessions/{sessionId}" }
}));

// 对话入口：一个 sessionId 即一个会话，多轮上下文由 SessionStore 维护
app.MapPost("/chat", async (ChatRequest req, SessionManager sessions, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(req.SessionId) || string.IsNullOrWhiteSpace(req.Message))
        return Results.BadRequest(new { error = "sessionId 与 message 不能为空" });

    var reply = await sessions.AskAsync(req.SessionId, req.Message, ct);
    return Results.Ok(new ChatResponse(req.SessionId, reply, DateTimeOffset.Now));
});

// 查看当前挂载的能力插件（Planner 可见的工具列表）
app.MapGet("/tools", (ToolExecutor exec) => Results.Ok(
    exec.RegisteredTools.Select(t => new { t.Name, t.Description })));

// 查看会话历史（状态层 Session Store 的只读视图）
app.MapGet("/sessions/{sessionId}", async (string sessionId, ISessionStore store, CancellationToken ct) =>
{
    var s = await store.GetOrCreateAsync(sessionId, ct);
    return Results.Ok(new
    {
        sessionId = s.Id,
        messageCount = s.Messages.Count,
        updatedAt = s.UpdatedAt,
        messages = s.Messages.Select(m => new { role = m.Role.ToString(), m.Content, tool = m.ToolName })
    });
});

app.Run();

// --- 请求 / 响应契约 ---
public record ChatRequest(string SessionId, string Message);
public record ChatResponse(string SessionId, string Reply, DateTimeOffset ServerTime);
