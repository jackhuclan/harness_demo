"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { sendChatStream, listTools, listSessions, fetchSessionMessages, SessionSummary } from "@/lib/api";
import { Markdown } from "@/lib/Markdown";

interface Message {
  role: "user" | "assistant";
  content: string;
}

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
  const { dark, toggle } = useTheme();
  const [sessionId, setSessionId] = useState("");
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [tools, setTools] = useState<{ name: string; description: string }[]>([]);
  const [sessions, setSessions] = useState<SessionSummary[]>([]);
  const scrollRef = useRef<HTMLDivElement>(null);
  const abortRef = useRef<AbortController | null>(null);

  // 初始化：取/建当前 sessionId
  useEffect(() => {
    const existing = sessionStorage.getItem("harness_session");
    const next = existing ?? `web-${Date.now().toString(36)}`;
    sessionStorage.setItem("harness_session", next);
    setSessionId(next);
  }, []);

  // 刷新侧边栏会话列表
  const refreshSessions = useCallback(() => {
    listSessions().then(setSessions).catch(() => setSessions([]));
  }, []);

  useEffect(() => {
    listTools().then(setTools).catch(() => setTools([]));
    refreshSessions();
  }, [refreshSessions]);

  // 切换会话时加载历史
  useEffect(() => {
    if (!sessionId) return;
    fetchSessionMessages(sessionId).then(setMessages).catch(() => setMessages([]));
  }, [sessionId]);

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
  }, [messages, loading]);

  function newChat() {
    const next = `web-${Date.now().toString(36)}`;
    sessionStorage.setItem("harness_session", next);
    setSessionId(next);
    setMessages([]);
  }

  function switchSession(id: string) {
    if (id === sessionId) return;
    sessionStorage.setItem("harness_session", id);
    setSessionId(id);
  }

  async function handleSend(text?: string) {
    const content = (text ?? input).trim();
    if (!content || loading || !sessionId) return;
    setInput("");
    setMessages((m) => [...m, { role: "user", content }]);
    setLoading(true);
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
        next[lastIdx] = { role: "assistant", content: `⚠️ 请求失败：${e.message}` };
        return next;
      });
    } finally {
      setLoading(false);
      refreshSessions();
    }
  }

  const suggestions = [
    "帮我看下 LOT-2207 的统计结果",
    "看一下上午跑的数据怎么样（完整链路）",
    "分析下 ProductA_M1M2_Layer 这个处方",
    "帮我给 Foup2 slot5 建个处方",
    "机台性能怎么样？",
  ];

  return (
    <div className="flex h-full">
      {/* 侧边栏 */}
      <aside className="flex w-64 shrink-0 flex-col border-r border-slate-200 bg-white/70 p-3 dark:border-slate-700 dark:bg-slate-900/60">
        <button
          onClick={newChat}
          className="mb-3 rounded-lg bg-emerald-600 px-3 py-2 text-sm font-medium text-white transition hover:bg-emerald-500"
        >
          + 新建会话
        </button>
        <div className="mb-2 text-xs text-slate-400">历史会话</div>
        <div className="flex-1 space-y-1 overflow-y-auto">
          {sessions.length === 0 && (
            <div className="px-2 py-4 text-center text-xs text-slate-400">暂无历史会话</div>
          )}
          {sessions.map((s) => (
            <button
              key={s.sessionId}
              onClick={() => switchSession(s.sessionId)}
              className={`w-full truncate rounded-lg px-2 py-2 text-left text-xs transition ${
                s.sessionId === sessionId
                  ? "bg-emerald-500/20 text-emerald-700 dark:text-emerald-300"
                  : "text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"
              }`}
              title={s.preview}
            >
              <div className="truncate font-medium">{s.preview}</div>
              <div className="mt-0.5 text-[10px] text-slate-400">
                {s.messageCount} 条 · {new Date(s.updatedAt).toLocaleTimeString("zh-CN", { hour: "2-digit", minute: "2-digit" })}
              </div>
            </button>
          ))}
        </div>
      </aside>

      {/* 主聊天区 */}
      <div className="flex flex-1 flex-col p-4">
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
            <div key={i} className={`flex ${m.role === "user" ? "justify-end" : "justify-start"}`}>
              <div className={`max-w-[80%] rounded-2xl px-4 py-2 text-sm ${
                m.role === "user"
                  ? "bg-emerald-600 text-white whitespace-pre-wrap"
                  : "bg-slate-200 text-slate-900 dark:bg-slate-700 dark:text-slate-100"
              }`}>
                {m.role === "assistant" ? (
                  m.content ? <Markdown text={m.content} /> : (loading ? "▍" : "")
                ) : (
                  m.content
                )}
              </div>
            </div>
          ))}
        </div>

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
    </div>
  );
}
