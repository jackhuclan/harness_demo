# AGENTS.md

本文件为 AI Agent（及人类协作者）在本仓库工作时的约定。

## 项目概览

- **名称**：harness_demo —— Agent Harness 架构的 C# .NET 8 落地 Demo
- **仓库**：https://github.com/jackhuclan/harness_demo （main 分支）
- **架构**：按八层分层图（见 `docs/architecture.jpg`）组织 9 个类库 + 1 个 Web 入口
- **后端**：ASP.NET Core Minimal API，监听 `http://localhost:5000`，Swagger 在 `/swagger`
- **前端**：Next.js 14 (App Router) + React 18 + Tailwind CSS，目录 `chat-demo/`，dev 端口 `3000`

## 目录结构

```
src/
  HarnessDemo.Abstractions/   # 接口与模型契约（ILlmClient, ITool, ChatMessage...）
  HarnessDemo.AgentCore/      # Controller 循环：Planner 决策 -> 工具执行 -> 结果回灌
  HarnessDemo.Model/          # LLM 客户端：OpenAiLlmClient（真实）/ RuleBasedLlmClient（mock）
  HarnessDemo.Skills/         # 能力插件：CalculatorTool, TimeTool...
  HarnessDemo.Execution/      # 执行层：ToolExecutor 调度
  HarnessDemo.State/          # 状态层：SessionStore / MemoryStore / TaskStore
  HarnessDemo.Security/       # 安全层：SensitiveWordAuditor
  HarnessDemo.Session/        # 会话路由：Router / SessionManager
  HarnessDemo.Host/           # 入口层：Program.cs（Minimal API + DI 组装）
chat-demo/                    # Next.js 前端
docs/architecture.jpg         # 架构图
build.ps1 / build.cmd         # 一键构建
start.ps1 / start.bat         # 一键启动
.github/workflows/ci.yml      # GitHub Actions（build + 冒烟测试）
```

## 技术栈与版本

- .NET 8（net8.0），C# 12
- NuGet 包固定版本：
  - `OpenAI` 2.14.0（v2.x，注意类型名：`SystemChatMessage`/`UserChatMessage`/`AssistantChatMessage`/`ToolChatMessage`；`ChatTool.CreateFunctionTool` 第三参为 `BinaryData`；流式属性 `ContentUpdate.Text` / `ToolCallUpdates`）
  - `DotNetEnv` 3.2.0（读 `.env`）
  - `Swashbuckle.AspNetCore.Swagger` 10.3.1
- 前端：Next.js 14.2.x，React 18，Tailwind CSS 3，TypeScript
- 本地 LLM：vLLM 兼容端点，模型 `deepseek-v4.1-flash`（reasoning 模型，思考链在 reasoning 字段）

## 构建与运行

```powershell
# 一键构建（后端 Release + 前端 next build）
.\build.cmd

# 一键启动（后端 5000 + 前端 3000，各开窗口）
.\start.bat

# 手动
dotnet run --project src/HarnessDemo.Host --urls http://localhost:5000
cd chat-demo; npm install; npm run dev
```

## 配置

- `.env` 在仓库根目录，已 gitignore。关键字段：
  - `USE_REAL_LLM=true|false`
  - `LLM_ENDPOINT=http://192.168.3.184:3001/v1`
  - `LLM_API_KEY=sk-...`
  - `LLM_MODEL=deepseek-v4.1-flash`
  - `LLM_MAX_TOKENS=4096`
- 前端后端地址：环境变量 `NEXT_PUBLIC_API_BASE`（默认 `http://localhost:5000`）

## API 端点

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/` | 服务发现 / 健康检查 |
| GET | `/tools` | 已挂载工具列表 |
| POST | `/chat` | 非流式对话 |
| POST | `/chat/stream` | SSE 流式对话（`text/event-stream`） |
| GET | `/sessions/{id}` | 会话历史只读视图 |

CORS：仅允许 `http://localhost:3000` 和 `http://127.0.0.1:3000`。

## 代码规范

- **分层依赖方向**：Host → AgentCore/Session → Execution/Skills → Model/State → Abstractions。
  下层不反向依赖上层；跨层通信只走 Abstractions 里的接口。
- **DI**：在 `Host/Program.cs` 集中注册；用 `AddSingleton`（Demo 无并发要求）。
- **工具插件**：实现 `ITool` 接口，在 Host 里 `AddSingleton<ITool, XxxTool>()`，
  `ToolExecutor` 会自动扫描注册。
- **LLM 切换**：只改 Host 里 `ILlmClient` 的注册，业务代码不动。
- **异步**：全链路 `async/await`，入口方法带 `CancellationToken`。
- **错误处理**：模型层 catch 后返回降级文本，不让异常冒泡到 HTTP 层；
  安全审计拦截直接返回 `⛔` 前缀。
- **前端**：
  - 默认 light 主题，`dark` class 切换，偏好存 `localStorage`（key: `harness_theme`）
  - sessionId 存 `sessionStorage`（key: `harness_session`），只在 `useEffect` 里生成（避免水合错误）
  - 流式用 `fetch + ReadableStream.getReader()` 消费 SSE，不要用 EventSource（不支持 POST）

## Git 约定

- main 分支保护，PR 合入；提交信息用 Conventional Commits：
  - `feat: ...` 新功能
  - `fix: ...` 修 bug
  - `refactor: ...` 重构
  - `docs: ...` 文档
- 每次改动跑通 `build.ps1` 再推送。
- 不要提交：`bin/`、`obj/`、`node_modules/`、`.next/`、`.env`、`*.user`。

## 已知坑（踩过的）

1. **Mock LLM 死循环**：工具结果回灌后必须补一条 `Tool` 角色消息，否则模型会重复调用同一工具。
2. **SafeEval 解析器遇空格中断**：`12*(3+4)/2` 之前只算 12，需正确处理空白。
3. **OpenAI SDK v2.x 类型名变了**：不是 `ChatMessage`，而是按角色分的 `SystemChatMessage` 等。
4. **前端水合错误**：`sessionId` 在 `useState` 初始化时随机生成会导致 SSR/CS 不一致，必须放 `useEffect`。
5. **CORS**：后端必须配 `UseCors()` 且放在路由之前，否则浏览器从 :3000 调 :5000 会被拦。
