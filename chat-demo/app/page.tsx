"use client";

import { useEffect, useRef, useState } from "react";
import { sendChatStream, listTools } from "@/lib/api";
import { Markdown } from "@/lib/Markdown";

interface Message {
  role: "user" | "assistant";
  content: string;
}

// 每个浏览器 tab 一个稳定的 sessionId，保持多轮上下文
function useSessionId() {
  const [id, setId] = useState("");
  useEffect(() => {
    const existing = sessionStorage.getItem("harness_session");
    const next = existing ?? `web-${Date.now().toString(36)}`;
    sessionStorage.setItem("harness_session", next);
    setId(next);
  }, []);
  return id;
}

// 主题：默认 light，可切换 dark
function useTheme() {
  const [dark, setDark] = useState(false);
  useEffect(() => {
    const saved = localStorage.getItem("harness_theme");
    const isDark = saved === "dark";
    setDark(isDark);
    document.documentElement.classList.toggle("dark", isDark);
  }, []);
  function toggle() {
    const next = !dark;
    setDark(next);
    document.documentElement.classList.toggle("dark", next);
    localStorage.setItem("harness_theme", next ? "dark" : "light");
  }
  return { dark, toggle };
}

export default function ChatPage() {
  const sessionId = useSessionId();
  const { dark, toggle } = useTheme();
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [tools, setTools] = useState<{ name: string; description: string }[]>([]);
  const scrollRef = useRef<HTMLDivElement>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    listTools().then(setTools);
  }, []);

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
  }, [messages, loading]);

  async function handleSend(text?: string) {
    const content = (text ?? input).trim();
    if (!content || loading || !sessionId) return;
    setInput("");
    setMessages((m) => [...m, { role: "user", content }]);
    setLoading(true);

    // 先插入一个空的 assistant 气泡，流式填充
    setMessages((m) => [...m, { role: "assistant", content: "" }]);
    const lastIdx = messages.length + 1;

    try {
      abortRef.current?.abort();
      const ac = new AbortController();
      abortRef.current = ac;
      await sendChatStream(
        sessionId,
        content,
        (delta) => {
          setMessages((m) => {
            const next = [...m];
            next[lastIdx] = { role: "assistant", content: next[lastIdx].content + delta };
            return next;
          });
        },
        ac.signal
      );
    } catch (e: any) {
      setMessages((m) => {
        const next = [...m];
        next[lastIdx] = {
          role: "assistant",
          content: `⚠️ 请求失败：${e.message}（后端是否启动在 http://localhost:5000？）`,
        };
        return next;
      });
    } finally {
      setLoading(false);
    }
  }

  const suggestions = [
    "帮我看下 LOT-2207 的统计结果",
    "看一下上午跑的数据怎么样（完整链路）",
    "分析下 ProductA_M1M2_Layer 这个处方",
    "帮我给 Foup2 slot5 建个处方",
    "机台性能怎么样？",
    "看下粗聚焦失败怎么诊断",
    "确认执行处置并复测",
    "光源还能用多久？",
  ];

  return (
    <div className="mx-auto flex h-full max-w-3xl flex-col p-4">
      {/* 顶栏 */}
      <header className="mb-4 flex items-center justify-between rounded-xl bg-white/70 px-4 py-3 shadow-sm backdrop-blur dark:bg-slate-800/60">
        <div>
          <h1 className="text-lg font-semibold">Agent Harness Chat</h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            session: <code className="text-slate-700 dark:text-slate-300">{sessionId}</code>
          </p>
        </div>
        <div className="flex items-center gap-3">
          <details className="hidden text-right text-xs text-slate-500 dark:text-slate-400 sm:block">
            <summary className="cursor-pointer select-none">已挂载工具 ({tools.length})</summary>
            <div className="mt-1 flex max-w-xs flex-wrap justify-end gap-1">
              {tools.map((t) => (
                <span key={t.name} className="rounded bg-emerald-500/20 px-1.5 py-0.5 text-emerald-600 dark:text-emerald-300">
                  {t.name}
                </span>
              ))}
            </div>
          </details>
          <button
            onClick={toggle}
            title={dark ? "切换到浅色" : "切换到深色"}
            className="rounded-lg border border-slate-300 px-2.5 py-1.5 text-sm dark:border-slate-600"
          >
            {dark ? "🌙" : "☀️"}
          </button>
        </div>
      </header>

      {/* 消息区 */}
      <div
        ref={scrollRef}
        className="mb-4 flex-1 space-y-3 overflow-y-auto rounded-xl bg-white/50 p-4 shadow-sm dark:bg-slate-800/40"
      >
        {messages.length === 0 && (
          <div className="pt-10 text-center text-slate-500 dark:text-slate-400">
            <p className="mb-3">开始对话吧，试试：</p>
            <div className="flex flex-wrap justify-center gap-2">
              {suggestions.map((s) => (
                <button
                  key={s}
                  onClick={() => handleSend(s)}
                  className="rounded-full border border-slate-300 px-3 py-1.5 text-sm text-slate-600 transition hover:border-emerald-500 hover:text-emerald-600 dark:border-slate-600 dark:text-slate-300 dark:hover:border-emerald-400 dark:hover:text-emerald-300"
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
              className={`max-w-[80%] rounded-2xl px-4 py-2 text-sm ${
                m.role === "user"
                  ? "bg-emerald-600 text-white whitespace-pre-wrap"
                  : "bg-slate-200 text-slate-900 dark:bg-slate-700 dark:text-slate-100"
              }`}
            >
              {m.role === "assistant" ? (
                m.content ? <Markdown text={m.content} /> : (loading ? "▍" : "")
              ) : (
                m.content
              )}
            </div>
          </div>
        ))}
      </div>

      {/* 输入区 */}
      <div className="flex gap-2">
        <input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && handleSend()}
          placeholder="输入消息，回车发送…"
          className="flex-1 rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900 placeholder-slate-400 outline-none focus:border-emerald-500 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:placeholder-slate-500 dark:focus:border-emerald-400"
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
