# OVL Agent 分期实施计划

依据 `docs/OVL_Agent_PRD.md`（22 个 FR / 30 个 AC / 42 个 TC），映射到现有 harness 架构。

## 架构映射

| PRD 层 | 现有项目 | 扩展点 |
|---|---|---|
| 用户意图层 | `chat-demo/` 前端 | 结构化结果卡片、确认/介入按钮 |
| LLM 编排层 | `HarnessDemo.AgentCore/Agent.cs` | 规则路由（离线降级）+ LLM function calling |
| 设备侧 Skill | `HarnessDemo.Skills/` | 新增 OVL 系列 ITool |
| 状态/记忆 | `HarnessDemo.State/` | 处方库、LOT 数据、历史优化/诊断、诊断记忆 |
| 安全/介入 | `HarnessDemo.Security/` | 关键步骤暂停（Human-in-the-loop） |
| 入口 | `HarnessDemo.Host/Program.cs` | 新增 OVL 专用端点 |

## 分期

### P0（Must，本期）—— 核心闭环可演示
| FR | 工具/实现 |
|---|---|
| FR-SYS-001 | 规则意图路由（关键词匹配统计/异常/分析/诊断/性能） |
| FR-SYS-002 | 复用 MemoryStore，写操作过程与经验 |
| FR-SYS-003 | 关键步骤返回 `requiresConfirmation=true`，前端按钮确认 |
| FR-MGT-004 | `WaferLoadTool`：模拟 LOAD，返回 Wafer ID/规格 |
| FR-MGT-002 | `RecipeAnalyzeTool`：四部分输出（基本信息/运行统计/失败归类/建议） |
| FR-PDA-001 | `OvlStatisticsTool`：X/Y 3σ、片内最大、TIS 3σ、超规告警 |
| FR-PDA-002 | `OvlAnomalyTool`：失败定位 + 根因研判（Wafer 9 示例） |
| FR-TMA-001 | `ToolPerformanceTool`：重复性/TIS/MAM + spec 判定 + 超标原因排序 |
| FR-TDI-001 | `OvlDiagnosisTool`：报错识别 + log 根因（精对准/粗聚焦） |

### P1（Should）—— 完整分析链路
| FR | 工具 |
|---|---|
| FR-PDA-003 | `BudgetDecompTool`：Raw/W1F1/W3F3/CPE/Residual 占比 |
| FR-PDA-004 | `AttributionTool`：field-to-field 残差 + R² |
| FR-PDA-005 | `DecisionAdviceTool`：模型/采样/工艺建议 |
| FR-PDA-006 | `R2RCompTool`：下发校正 + 阻尼 + 闭环状态 |
| FR-PDA-007 | 多技能串联：Agent 按意图链调用 ≥3 工具 |
| FR-MGT-003 | `RecipeOptimizeTool`：知识库推荐（成功率/预期改善） |
| FR-TMA-002 | `ToolTrendTool`：按周趋势 + spec 逼近判定 |
| FR-TMA-003 | `HardwareHealthTool`：光源衰减拟合 + 剩余寿命 |
| FR-TDI-002 | 介入确认后执行处置 + 复测 |
| FR-TDI-003 | `ExperienceLearnTool`：写入诊断记忆，后续优先匹配 |

### P2（Could，后续）
| FR | 说明 |
|---|---|
| FR-MGT-001 | 13 步建方流程（步骤条 UI，逐步确认） |
| FR-CSK-001/002 | 自定义技能习得（自然语言规则解析入库） |

## 测试
- `HarnessDemo.Tests/`（xUnit）：每个 P0/P1 工具一组用例，覆盖正例 + 关键负例，对齐 TC 编号。
- 前端：`chat-demo` 内置示例问题按钮，点一下跑通 P0/P1 链路。

## 数据
- 全部 mock：seeded in-memory 仓库（`OvlDataStore`），数值与 PRD 示例一致（LOT-2207、Wafer 9、ProductA_M1M2_Layer、光源衰减等）。
- 接口层留好替换点：未来接真实设备报表时换 `OvlDataStore` 实现即可。
