using HarnessDemo.Abstractions;
using HarnessDemo.AgentCore;
using HarnessDemo.Execution;
using HarnessDemo.Model;
using HarnessDemo.Security;
using HarnessDemo.Session;
using HarnessDemo.Skills;
using HarnessDemo.State;

// =====================================================================
// Agent Harness Demo —— 入口层（对应架构图 Web/API/Bot/Voice 入口）
// 这里是组装根（Composition Root）：手工把各层 wiring 在一起。
// 生产环境可换成 Microsoft.Extensions.DependencyInjection。
// =====================================================================

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("==============================================");
Console.WriteLine("       Agent Harness (C#)  Demo");
Console.WriteLine("  分层：入口 -> 会话路由 -> AgentCore -> 能力/执行 -> 状态/模型/安全");
Console.WriteLine("==============================================");

// --- 执行层：注册能力层工具插件 ---
var executor = new ToolExecutor();
executor.Register(new CalculatorTool());
executor.Register(new TimeTool());

// --- 状态层：各类存储 ---
var sessionStore = new InMemorySessionStore();
var memoryStore = new InMemoryMemoryStore();
var taskStore = new InMemoryTaskStore();

// --- 模型层 / 安全层 / Agent Core ---
var llm = new RuleBasedLlmClient();
var auditor = new SensitiveWordAuditor();
var contextMgr = new ContextManager();
var agent = new Agent(llm, executor, sessionStore, memoryStore, auditor, contextMgr);

// --- 会话与路由层 ---
var router = new Router();
var sessionMgr = new SessionManager(sessionStore, agent, router);

// --- 跑一组 demo 对话脚本，观察每层链路日志 ---
const string sessionId = "demo-session-001";

string[] script =
{
    "现在几点了？",                              // 路由到 get_time 工具
    "请计算 12 * (3 + 4) / 2",                  // 路由到 calculator 工具
    "你好，介绍一下你自己",                       // 模型直接回答
    "帮我查询 secret 密码本",                    // 被安全层拦截
};

foreach (var q in script)
{
    Console.WriteLine($"\n[User] {q}");
    var answer = await sessionMgr.AskAsync(sessionId, q);
    Console.WriteLine($"[Agent] {answer}");
}

Console.WriteLine("\n==============================================");
Console.WriteLine("Demo 结束。退出。");
