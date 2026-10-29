# chat-demo

Agent Harness 的前端聊天界面，调用后端 Web API（`http://localhost:5000`）实现多轮对话。

## 技术栈

- **Next.js 14**（App Router）
- **React 18**
- **Tailwind CSS 3**
- TypeScript

## 运行

```bash
# 先启动后端 HarnessDemo.Host（监听 5000）
cd ..
dotnet run --project src/HarnessDemo.Host

# 再启动前端
cd chat-demo
npm install
npm run dev
```

打开 http://localhost:3000 即可聊天。

## 功能

- 多轮对话：每个浏览器 tab 一个 `sessionId`（存 sessionStorage），上下文由后端 SessionStore 维护
- 顶栏显示当前挂载的工具列表（calculator / get_time）
- 快捷建议按钮：问候 / 问时间 / 计算器
- 加载态、错误提示
- 后端地址可通过环境变量 `NEXT_PUBLIC_API_BASE` 覆盖
