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
  const res = await fetch(`${API_BASE}/tools`);
  if (!res.ok) return [];
  return res.json();
}
