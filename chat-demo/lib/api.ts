// 后端 Web API 地址（对应 HarnessDemo.Host，监听 5000）
const API_BASE = process.env.NEXT_PUBLIC_API_BASE || "http://localhost:5000";

export interface ChatResponse {
  sessionId: string;
  reply: string;
  serverTime: string;
}

export async function sendChat(
  sessionId: string,
  message: string
): Promise<ChatResponse> {
  const res = await fetch(`${API_BASE}/chat`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ sessionId, message }),
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
}

export async function listTools(): Promise<{ name: string; description: string }[]> {
  try {
    const res = await fetch(`${API_BASE}/tools`);
    if (!res.ok) return [];
    return await res.json();
  } catch {
    return [];
  }
}

export interface SessionSummary {
  sessionId: string;
  messageCount: number;
  updatedAt: string;
  preview: string;
}

export async function listSessions(): Promise<SessionSummary[]> {
  try {
    const res = await fetch(`${API_BASE}/sessions`);
    if (!res.ok) return [];
    return await res.json();
  } catch {
    return [];
  }
}

export async function fetchSessionMessages(sessionId: string): Promise<Message[]> {
  try {
    const res = await fetch(`${API_BASE}/sessions/${sessionId}`);
    if (!res.ok) return [];
    const data = await res.json();
    return (data.messages ?? []).map((m: any) => ({
      role: m.role === "User" ? "user" : m.role === "Assistant" ? "assistant" : "assistant",
      content: m.content,
    }));
  } catch {
    return [];
  }
}

interface Message {
  role: "user" | "assistant";
  content: string;
}

/** 流式对话：逐段调用 onDelta，返回完整文本 */
export async function sendChatStream(
  sessionId: string,
  message: string,
  onDelta: (delta: string) => void,
  signal?: AbortSignal
): Promise<void> {
  const res = await fetch(`${API_BASE}/chat/stream`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ sessionId, message }),
    signal,
  });
  if (!res.ok || !res.body) throw new Error(`HTTP ${res.status}`);

  const reader = res.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true });

    // SSE 以 \n\n 分隔事件
    const events = buffer.split("\n\n");
    buffer = events.pop() ?? "";
    for (const evt of events) {
      const line = evt.trim();
      if (!line.startsWith("data:")) continue;
      const payload = line.slice(5).trim();
      if (payload === "[DONE]") return;
      try {
        const json = JSON.parse(payload);
        if (json.delta) onDelta(json.delta);
      } catch {
        // 忽略不完整 JSON
      }
    }
  }
}
