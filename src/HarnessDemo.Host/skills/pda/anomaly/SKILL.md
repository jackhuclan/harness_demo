---
name: ovl-pda-anomaly
description: OVL 生产数据分析 - 异常分析。当用户询问失败原因、为什么 fail、根因研判时使用。触发词："为什么 fail"、"异常"、"根因"、"Wafer 9"。
allowed-tools: [ovl_anomaly]
---

# 技能 · 异常分析

## 所属 Agent
生产数据分析（pda）

## 触发示例
- 看下刚才跑的数据为什么大多都是 fail
- Wafer 9 为什么失败？
- 这批片异常根因是什么？

## 执行工具
`ovl_anomaly`（无入参）

## 输出内容
- 失败范围定位（哪些 Wafer / Field 失败率高）
- 数据/图像/log 综合根因研判
- 失败归类统计（OVL/TIS/聚焦/对准占比）
- 初步处置建议

## 示例输出
```
## 🔍 异常分析报告

- 失败 Wafer：W0009（失败率 82%）
- 根因：粗聚焦信号异常，Focus offset 漂移 0.8 μm
- 失败归类：聚焦失败 60% / OVL 超限 25% / 其他 15%
- 建议：检查 Focus Travel 行程，必要时重新校准
```
