---
name: ovl-pda-budget
description: OVL 生产数据分析 - 性能分解 Budget。当用户询问数据差异贡献项、误差预算分解时使用。触发词："差异有多大"、"主要贡献项"、"budget"、"分解"。
---

# 技能 · 性能分解 Budget

## 所属 Agent
生产数据分析（pda）

## 触发示例
- 上午这批 Lot 的数据差异有多大？主要贡献项是什么？
- Raw/W1F1/W3F3/CPE/Residual 各占多少？
- 哪一项误差贡献最大？

## 执行工具
`budget_decomp`（无入参）

## 输出内容
- Raw residual 3σ
- W1F1（Wafer-to-Wafer）贡献
- W3F3（Field-to-Field）贡献
- CPE（晶圆平差误差）贡献
- Residual（模型残差）贡献
- 最大贡献项排序 + 占比

## 示例输出
```
## 📈 误差预算分解

| 项 | 3σ (nm) | 占比 |
|---|---|---|
| Raw | 0.42 | 100% |
| W1F1 | 0.08 | 19% |
| W3F3 | 0.15 | 36% ← 最大 |
| CPE | 0.06 | 14% |
| Residual | 0.21 | 50% |
```
