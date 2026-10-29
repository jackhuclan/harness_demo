---
name: ovl-recipe-analyze
description: OVL 处方管理 - 处方分析。当用户要分析现有处方运行情况、失败归类、优化建议时使用。触发词："分析下这个处方"、"ProductA_M1M2_Layer"、"处方运行情况"。
---

# 技能 · 处方分析

## 所属 Agent
处方管理（recipe）

## 触发示例
- 分析下 ProductA_M1M2_Layer 这个处方
- 这个处方最近跑的怎么样？
- 失败原因是什么？

## 执行工具
`recipe_analyze`（入参：`{recipeName}`）

## 输出内容
### 1. 处方基本信息概况
- 产品/层：ProductA · M1M2 · Layer（套刻层）
- 标记类型：BIB 标记 ｜ OVL Model: linear
- TIS 模式：All Wafer ｜ Auto Retrain Test: 关闭
- 所属机台：OM800 ｜ 创建于 2026-03-12

### 2. 处方运行情况（近 30 天）
- 已量测：42 Lot ｜ 980 Wafer
- 测点成功率：99.9%（失败点 100 / 总 98,000）
- 硅片成功率：99.8%（失败 2 / 成功 978）

### 3. 测点失败原因归类
| 原因 | 次数 | 占比 |
|---|---|---|
| OVL 超限 | 38 | 38.0% |
| TIS 超限 | 20 | 20.0% |
| Qmerit 超限 | 18 | 18.0% |
| 聚焦失败 | 14 | 14.0% |
| 对准失败 | 10 | 10.0% |

### 4. 优化建议
① TIS 模式改为 First Wafer，省去每片 TIS 量测
② 开启 Auto Retrain Test，应对跨 Lot 漂移
