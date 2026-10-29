import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Agent Harness Chat",
  description: "Frontend demo for the Agent Harness Web API",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="zh-CN">
      <body>{children}</body>
    </html>
  );
}
