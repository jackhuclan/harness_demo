---
name: ovl-pda-chain
description: OVL 生产数据分析 - 多技能串联。当用户想一次性看完整分析链路（统计→异常→分解→归因→建议）时使用。触发词："这批数据怎么样"、"完整分析"、"看一下上午跑的"、"链路"。
---

# 技能 · 多技能串联

## 所属 Agent
生产数据分析（pda）

## 触发示例
- 看一下上午跑的这批数据怎么样
- 完整分析一下 LOT-2207
- 给我一份完整的 OVL 报告

## 执行工具
`ovl_chain`（无入参）

## 串联链路
1. `ovl_statistics` - 数据统计
2. `ovl_anomaly` - 异常分析
3. `budget_decomp` - 误差分解
4. `attribution` - 归因分析
5. `decision_advice` - 决策建议

## 输出内容
五段式完整报告，从统计到建议一条龙输出。
