# Agent Harness Demo (C#)

按 `Agent Harness 架构图` 落地的可运行 C# 骨架，演示一个智能体从「入口 → 会话路由 → 核心引擎循环 → 工具执行 → 状态持久化」的完整链路。

## 解决方案结构（与架构图分层一一对应）

| 架构图层 | 项目 | 职责 |
|---|---|---|
| 入口层 Agent Entry | `src/HarnessDemo.Host` | 控制台宿主 + 组装根（Composition Root） |
| 会话与路由层 Session & Routing | `src/HarnessDemo.Session` | `SessionManager`、`Router`（意图识别 / Agent 选择） |
| Agent Runtime Core | `src/HarnessDemo.AgentCore` | `Agent`（Planner→Controller 执行循环）、`ContextManager`（上下文组装 / 动态压缩） |
| 能力层 Agent Skills | `src/HarnessDemo.Skills` | `CalculatorTool`（安全求值）、`TimeTool` |
| 执行层 Execution | `src/HarnessDemo.Execution` | `ToolExecutor`（注册表 / 超时 / 异常重试 / 结果收集） |
| 状态层 State | `src/HarnessDemo.State` | 内存版 Session / Memory / Task Store |
| 模型层 Model | `src/HarnessDemo.Model` | `RuleBasedLlmClient`（规则模拟 LLM，生产可换 OpenAI/通义/文心） |
| 安全层 Security | `src/HarnessDemo.Security` | `SensitiveWordAuditor`（入口前置敏感词闸门） |
| 跨层契约 | `src/HarnessDemo.Abstractions` | 所有接口（`ILlmClient` / `ITool` / `IToolExecutor` / `ISessionStore` …） |

## 运行

```powershell
dotnet build
dotnet run --project src/HarnessDemo.Host
```

（本机 dotnet 不在 PATH 时用全路径：`& "C:\Program Files\dotnet\dotnet.exe" run --project src/HarnessDemo.Host`）

## 核心执行循环（Agent Core）

```
User 输入
  → Security.Auditor 内容审计（命中黑名单直接拦截）
  → SessionStore 加载历史
  → 循环（最多 6 跳）：
      ContextManager 组装 System + 最近 10 条历史
      → LLM.Planner 决策：需要工具？
          是 → ToolExecutor 调度（超时 5s，失败重试 1 次）→ 结果回灌上下文 → 继续
          否 → 跳出，生成最终答复
  → SessionStore 持久化 + MemoryStore 写入长期记忆
```

## 扩展点

- **换真实大模型**：实现 `ILlmClient`，内部调用 OpenAI / Azure / 通义 SDK 的 function calling，返回 `LlmDecision`。
- **加工具**：实现 `ITool`，在 Host 里 `executor.Register(new MyTool())`。
- **换存储**：实现 `ISessionStore` / `IMemoryStore`，背后接 Redis / 向量库 / 数据库。
- **多 Agent**：在 `Router.Route` 里按意图路由到不同 `Agent` 实例。
