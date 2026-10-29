---
name: ovl-pda-attribution
description: OVL 生产数据分析 - 归因分析。当用户询问 overlay 偏大根因、field-to-field 残差分析时使用。触发词："overlay 偏大"、"根因"、"归因"、"R²"。
---

# 技能 · 归因分析

## 所属 Agent
生产数据分析（pda）

## 触发示例
- 为什么这批 overlay 偏大，根因是什么？
- field-to-field 残差分布如何？
- 边缘场为什么偏差大？

## 执行工具
`attribution`（无入参）

## 输出内容
- field-to-field 残差热力分布描述
- R² 拟合优度证据（线性模型 vs 高阶模型）
- 边缘场 vs 中心场偏差对比
- 根因定位（CPE / 对准标记退化 / 镜头畸变）

## 示例输出
```
## 🎯 归因分析

- 边缘场平均残差 0.18 nm，中心场 0.06 nm
- 线性模型 R² = 0.72，高阶模型 R² = 0.91
- 根因：镜头边缘畸变 + CPE 偏大
- 建议：升级为 3rd-order model 补偿边缘畸变
```
