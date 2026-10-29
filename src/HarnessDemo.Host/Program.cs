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

// --- OVL Agent 演示工具（FR-MGT/PDA/TMA/TDI）---
builder.Services.AddSingleton<OvlDataStore>();
// 先注册具体类型（MultiSkillChainTool 等需要注入具体工具）
builder.Services.AddSingleton<WaferLoadTool>();
builder.Services.AddSingleton<RecipeAnalyzeTool>();
builder.Services.AddSingleton<OvlStatisticsTool>();
builder.Services.AddSingleton<OvlAnomalyTool>();
builder.Services.AddSingleton<ToolPerformanceTool>();
builder.Services.AddSingleton<OvlDiagnosisTool>();
builder.Services.AddSingleton<BudgetDecompTool>();
builder.Services.AddSingleton<AttributionTool>();
builder.Services.AddSingleton<DecisionAdviceTool>();
builder.Services.AddSingleton<R2RCompTool>();
builder.Services.AddSingleton<RecipeOptimizeTool>();
builder.Services.AddSingleton<ToolTrendTool>();
builder.Services.AddSingleton<HardwareHealthTool>();
builder.Services.AddSingleton<ExperienceLearnTool>();
builder.Services.AddSingleton<MultiSkillChainTool>();
builder.Services.AddSingleton<DispositionConfirmTool>();
builder.Services.AddSingleton<RecipeCreateTool>();
builder.Services.AddSingleton<CustomSkillLearnTool>();
// 再把它们登记为 ITool（供 ToolExecutor 扫描）
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<WaferLoadTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<RecipeAnalyzeTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<OvlStatisticsTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<OvlAnomalyTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<ToolPerformanceTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<OvlDiagnosisTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<BudgetDecompTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<AttributionTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<DecisionAdviceTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<R2RCompTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<RecipeOptimizeTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<ToolTrendTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<HardwareHealthTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<ExperienceLearnTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<MultiSkillChainTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<DispositionConfirmTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<RecipeCreateTool>());
builder.Services.AddSingleton<ITool>(sp => sp.GetRequiredService<CustomSkillLearnTool>());

// --- 会话与路由层 ---
builder.Services.AddSingleton<Router>();
builder.Services.AddSingleton<SessionManager>();

// --- Swagger / OpenAPI 文档 ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- CORS：允许前端 (Next.js :3000) 跨域调用 ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// CORS 必须放在路由/端点之前
app.UseCors();

// Swagger 中间件：暴露 /swagger/v1/swagger.json 和 /swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Agent Harness API v1");
    c.RoutePrefix = "swagger";
});

// 把 DI 注册的能力层插件登记进执行层注册表
var executor = app.Services.GetRequiredService<ToolExecutor>();
foreach (var tool in app.Services.GetServices<ITool>())
    executor.Register(tool);

// --- 入口层：HTTP API ---

// 服务发现 / 健康检查
app.MapGet("/", () => Results.Ok(new
{
    name = "Agent Harness API",
    swagger = "/swagger",
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

// 流式对话：SSE (text/event-stream)，逐字返回模型输出
app.MapPost("/chat/stream", async (ChatRequest req, Agent agent, HttpContext ctx) =>
{
    if (string.IsNullOrWhiteSpace(req.SessionId) || string.IsNullOrWhiteSpace(req.Message))
    {
        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsync("{\"error\":\"sessionId 与 message 不能为空\"}");
        return;
    }
    ctx.Response.Headers["Content-Type"] = "text/event-stream; charset=utf-8";
    ctx.Response.Headers["Cache-Control"] = "no-cache";
    ctx.Response.Headers["X-Accel-Buffering"] = "no";

    var writer = ctx.Response.BodyWriter;
    try
    {
        await foreach (var chunk in agent.StreamRunAsync(req.SessionId, req.Message, ctx.RequestAborted))
        {
            // SSE 格式：data: <json>\n\n
            var payload = System.Text.Json.JsonSerializer.Serialize(new { delta = chunk });
            var bytes = System.Text.Encoding.UTF8.GetBytes($"data: {payload}\n\n");
            await writer.WriteAsync(bytes, ctx.RequestAborted);
            await writer.FlushAsync(ctx.RequestAborted);
        }
        // 结束标记
        var done = System.Text.Encoding.UTF8.GetBytes("data: [DONE]\n\n");
        await writer.WriteAsync(done, ctx.RequestAborted);
        await writer.FlushAsync(ctx.RequestAborted);
    }
    catch (OperationCanceledException) { /* 客户端断开 */ }
});

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
