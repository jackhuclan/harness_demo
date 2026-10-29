---
name: ovl-tdi-focus
description: OVL 机台诊断修复 - 粗聚焦失败诊断。当用户询问粗聚焦为什么失败、Focus Travel 调整、Coarse Fail 时使用。触发词："粗聚焦失败"、"Foup3 slot7"、"Focus Travel"、"z 向搜索范围"。
allowed-tools: [ovl_diagnose, disposition_confirm]
---

# 技能 · 粗聚焦失败诊断

## 所属 Agent
机台诊断修复（tdi）

## 触发示例
- 刚才那片量测为什么粗聚焦失败了

## 执行工具
`ovl_diagnose`（入参：`{error:"粗聚焦失败"}`）

## 输出内容
识别到粗聚焦阶段失败，正在调取运行 log 与聚焦过程记录…

### 1. 报错 · 粗聚焦失败（Coarse Focus Fail，Foup3 · slot7）
量测 Foup3 slot7 时粗聚焦（Coarse Focus）反复失败，未进入精对准即中断。

### 2. 诊断 · 查看运行 log · 识别根因
log 显示 coarse focus 多次 retry 仍未锁定；该 wafer 标记存在高度/倾斜偏移，当前聚焦行程（Focus Travel）设置为 **50μm**，z 向搜索范围不足以覆盖该片标记，导致聚焦失败。

### 3. 处置 · 推荐修改聚焦行程
建议将该 wafer 的聚焦行程由 **50μm 调整为 80μm**，覆盖更大 z 范围后重新聚焦；属 run 参数调整，可直接下发。

### 用户介入
是否按建议修改聚焦行程并复测？（用户可介入）

底部按钮：「按建议修改聚焦行程并复测 ✓」
