import React from "react";

// 轻量 Markdown 渲染器：支持 ## 标题、**bold**、`code`、表格、列表、段落
export function Markdown({ text }: { text: string }) {
  const blocks = parseBlocks(text);
  return (
    <div className="space-y-2 text-sm leading-relaxed">
      {blocks.map((b, i) => renderBlock(b, i))}
    </div>
  );
}

type Block =
  | { type: "h2"; text: string }
  | { type: "p"; text: string }
  | { type: "list"; items: string[] }
  | { type: "table"; header: string[]; rows: string[][] };

function parseBlocks(text: string): Block[] {
  const lines = text.split("\n");
  const blocks: Block[] = [];
  let i = 0;
  while (i < lines.length) {
    const line = lines[i];
    if (!line.trim()) { i++; continue; }

    // 表格：连续以 | 开头的行
    if (line.trim().startsWith("|")) {
      const tableLines: string[] = [];
      while (i < lines.length && lines[i].trim().startsWith("|")) {
        tableLines.push(lines[i].trim());
        i++;
      }
      // 第二行是 |---|---| 分隔符
      if (tableLines.length >= 2 && /^\|[\s:|-]+\|$/.test(tableLines[1])) {
        const header = splitRow(tableLines[0]);
        const rows = tableLines.slice(2).map(splitRow);
        blocks.push({ type: "table", header, rows });
        continue;
      }
    }

    if (line.startsWith("## ")) {
      blocks.push({ type: "h2", text: line.slice(3) });
      i++;
      continue;
    }
    if (/^\d+\.\s/.test(line) || line.startsWith("- ")) {
      const items: string[] = [];
      while (i < lines.length && (/^\d+\.\s/.test(lines[i]) || lines[i].startsWith("- "))) {
        items.push(lines[i].replace(/^\d+\.\s/, "").replace(/^- /, ""));
        i++;
      }
      blocks.push({ type: "list", items });
      continue;
    }
    // 普通段落（合并连续非空行）
    let para = line;
    i++;
    while (i < lines.length && lines[i].trim() && !isBlockStart(lines[i])) {
      para += " " + lines[i].trim();
      i++;
    }
    blocks.push({ type: "p", text: para });
  }
  return blocks;
}

function isBlockStart(line: string) {
  return line.startsWith("## ") || line.startsWith("|") || /^\d+\.\s/.test(line) || line.startsWith("- ");
}

function splitRow(line: string): string[] {
  return line.replace(/^\|/, "").replace(/\|$/, "").split("|").map((s) => s.trim());
}

function renderBlock(b: Block, i: number): React.ReactNode {
  switch (b.type) {
    case "h2":
      return <h3 key={i} className="text-base font-semibold mt-2">{renderInline(b.text)}</h3>;
    case "p":
      return <p key={i}>{renderInline(b.text)}</p>;
    case "list":
      return (
        <ul key={i} className="list-disc pl-5 space-y-1">
          {b.items.map((it, j) => <li key={j}>{renderInline(it)}</li>)}
        </ul>
      );
    case "table":
      return (
        <div key={i} className="overflow-x-auto rounded-lg border border-slate-300 dark:border-slate-600">
          <table className="w-full text-xs">
            <thead className="bg-slate-100 dark:bg-slate-700">
              <tr>
                {b.header.map((h, j) => (
                  <th key={j} className="px-2 py-1.5 text-left font-semibold">{renderInline(h)}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {b.rows.map((row, j) => (
                <tr key={j} className="border-t border-slate-200 dark:border-slate-600">
                  {row.map((cell, k) => (
                    <td key={k} className="px-2 py-1.5">{renderInline(cell)}</td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      );
  }
}

// 行内渲染：**bold**、`code`
function renderInline(text: string): React.ReactNode {
  const parts = text.split(/(\*\*[^*]+\*\*|`[^`]+`)/g);
  return parts.map((p, i) => {
    if (p.startsWith("**") && p.endsWith("**")) {
      return <strong key={i}>{p.slice(2, -2)}</strong>;
    }
    if (p.startsWith("`") && p.endsWith("`")) {
      return <code key={i} className="rounded bg-slate-200 px-1 dark:bg-slate-600">{p.slice(1, -1)}</code>;
    }
    return <React.Fragment key={i}>{p}</React.Fragment>;
  });
}
