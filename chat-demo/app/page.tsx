"use client";

import { useEffect, useRef, useState } from "react";
import { sendChat, listTools } from "@/lib/api";

interface Message {
  role: "user" | "assistant";
  content: string;
}

// 每个浏览器 tab 一个稳定的 sessionId，保持多轮上下文
function useSessionId() {
  const [id] = useState(
    () =>
      (typeof window !== "undefined" &&
        sessionStorage.getItem("harness_session")) ||
      `web-${Date.now().toString(36)}`
  );
  useEffect(() => {
    sessionStorage.setItem("harness_session", id);
  }, [id]);
  return id;
}

export default function ChatPage() {
  const sessionId = useSessionId();
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [tools, setTools] = useState<{ name: string; description: string }[]>([]);
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    listTools().then(setTools);
  }, []);

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
  }, [messages, loading]);

  async function handleSend(text?: string) {
    const content = (text ?? input).trim();
    if (!content || loading) return;
    setInput("");
    setMessages((m) => [...m, { role: "user", content }]);
    setLoading(true);
    try {
      const res = await sendChat(sessionId, content);
      setMessages((m) => [...m, { role: "assistant", content: res.reply }]);
    } catch (e: any) {
      setMessages((m) => [
        ...m,
        { role: "assistant", content: `⚠️ 请求失败：${e.message}（后端是否启动在 http://localhost:5000？）` },
      ]);
    } finally {
      setLoading(false);
    }
  }

  const suggestions = [
    "你好，介绍一下你自己",
    "现在几点了？",
    "请调用计算器算 25 * 4 + 10",
  ];

  return (
    <div className="mx-auto flex h-full max-w-3xl flex-col p-4">
      {/* 顶栏 */}
      <header className="mb-4 flex items-center justify-between rounded-xl bg-slate-800/60 px-4 py-3 backdrop-blur">
        <div>
          <h1 className="text-lg font-semibold">Agent Harness Chat</h1>
          <p className="text-xs text-slate-400">
            session: <code className="text-slate-300">{sessionId}</code>
          </p>
        </div>
        <div className="hidden text-right text-xs text-slate-400 sm:block">
          <div className="mb-1">已挂载工具</div>
          {tools.map((t) => (
            <span key={t.name} className="ml-1 rounded bg-emerald-500/20 px-1.5 py-0.5 text-emerald-300">
              {t.name}
            </span>
          ))}
        </div>
      </header>

      {/* 消息区 */}
      <div
        ref={scrollRef}
        className="mb-4 flex-1 space-y-3 overflow-y-auto rounded-xl bg-slate-800/40 p-4"
      >
        {messages.length === 0 && (
          <div className="pt-10 text-center text-slate-400">
            <p className="mb-3">开始对话吧，试试：</p>
            <div className="flex flex-wrap justify-center gap-2">
              {suggestions.map((s) => (
                <button
                  key={s}
                  onClick={() => handleSend(s)}
                  className="rounded-full border border-slate-600 px-3 py-1.5 text-sm text-slate-300 transition hover:border-emerald-400 hover:text-emerald-300"
                >
                  {s}
                </button>
              ))}
            </div>
          </div>
        )}
        {messages.map((m, i) => (
          <div
            key={i}
            className={`flex ${m.role === "user" ? "justify-end" : "justify-start"}`}
          >
            <div
              className={`max-w-[80%] whitespace-pre-wrap rounded-2xl px-4 py-2 text-sm ${
                m.role === "user"
                  ? "bg-emerald-600 text-white"
                  : "bg-slate-700 text-slate-100"
              }`}
            >
              {m.content}
            </div>
          </div>
        ))}
        {loading && (
          <div className="flex justify-start">
            <div className="rounded-2xl bg-slate-700 px-4 py-2 text-sm text-slate-300">
              <span className="inline-block animate-pulse">正在思考…</span>
            </div>
          </div>
        )}
      </div>

      {/* 输入区 */}
      <div className="flex gap-2">
        <input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && handleSend()}
          placeholder="输入消息，回车发送…"
          className="flex-1 rounded-xl border border-slate-600 bg-slate-800 px-4 py-3 text-sm text-slate-100 placeholder-slate-500 outline-none focus:border-emerald-400"
        />
        <button
          onClick={() => handleSend()}
          disabled={loading || !input.trim()}
          className="rounded-xl bg-emerald-600 px-5 py-3 text-sm font-medium text-white transition hover:bg-emerald-500 disabled:cursor-not-allowed disabled:opacity-40"
        >
          发送
        </button>
      </div>
    </div>
  );
}
