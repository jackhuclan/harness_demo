---
name: ovl-pda-advice
description: OVL 生产数据分析 - 决策建议。当用户询问改进建议、怎么改进、工艺优化方向时使用。触发词："怎么改进"、"给个建议"、"改进方案"、"advice"。
allowed-tools: [decision_advice]
---

# 技能 · 决策建议

## 所属 Agent
生产数据分析（pda）

## 触发示例
- 针对这个问题该怎么改进、给个建议？
- 模型要升级吗？
- 采样方案要调整吗？

## 执行工具
`decision_advice`（无入参）

## 输出内容
- 模型升级建议（linear → 3rd-order / 高阶）
- 采样方案建议（First Wafer / All Wafer / 动态采样）
- 工艺改进建议（Focus offset / 亮度 / Culling）
- 预期改善幅度

## 示例输出
```
## 💡 决策建议

1. **模型升级**：linear → 3rd-order，预计 R² 从 0.72 提升到 0.91
2. **采样策略**：TIS 改为 First Wafer，节省 10% 量测时间
3. **工艺调整**：开启 Auto Retrain Test，应对跨 Lot 漂移
```
