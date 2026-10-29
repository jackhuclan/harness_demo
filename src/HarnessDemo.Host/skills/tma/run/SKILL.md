---
name: ovl-tma-run
description: OVL 机台监测分析 - 运行机台性能检测。当用户要求用 Recipe: Tool Performance 跑机台性能、加载硅片跑性能测试时使用。触发词："Foup1 slot2"、"Tool Performance"、"跑机台性能"、"运行监测"。
allowed-tools: [tool_performance, wafer_load]
---

# 技能 · 运行机台监测分析

## 所属 Agent
机台监测分析（tma）

## 触发示例
- 用 Foup1 的 slot2 运行 Recipe: Tool Performance 跑机台性能，以后都用该配置

## 执行工具
`tool_performance`（入参：`{foup:"Foup1", slot:2}`）

## 输出内容
### 1. 加线 · 自动加载晶圆
调度机台从 Foup1·slot2 自动取出并装载硅片（Wafer ID 读取完成）。

### 2. 处方 · 识别并运行处方
识别到 Recipe: Tool Performance，自动执行机台性能测量流程（重复性 / TIS / 模型残差）。

### 3. 结果 · 性能测试结果（首次，全在控）
| 指标 | 实测 | Spec | 判定 |
|---|---|---|---|
| OVL 重复性 (3σ) | 0.21 nm | ≤ 0.25 nm | ✅ 在控 |
| TIS Mean | 0.17 nm | 在控 | ✅ |
| TIS 3σ | 0.24 nm | 在控 | ✅ |
| 单点量测时间 (MAM) | 0.28s | 在控 | ✅ |

性能监测配置已更新至 Agent 记忆（后续默认调用 Recipe: Tool Performance，Foup1/slot2 配置已记录）。

### 4. 主动监测 · 重复性超标时主动提议排查
当重复性 (3σ) 升至 0.87 nm（远超 spec 0.25 nm）时，Agent 按历史诊断记录自动排序预测可能原因：

| 可能原因 | 历史匹配 | 成功率 | 预期改善 (3σ) | 优先级 |
|---|---|---|---|---|
| ① Stage 稳定性（Z 向/机械振动引入漂移） | 23 例 | 71% | 0.87 → 0.52 nm | 中 |
| ② SFO 漂移（聚焦标定偏离最优位置） | 38 例 | 86% | 0.87 → 0.21 nm | 高 |
| ③ 光源稳定性超标（功率/亮度波动） | 19 例 | 58% | 0.87 → 0.61 nm | 中 |
| ④ 光阑零位漂移（光阑零位偏差超容差） | 12 例 | 47% | 0.87 → 0.66 nm | 低 |

底部按钮：「确认并执行排查与优化 ✓」
