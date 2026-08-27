# Oracle 数据库答辩证据手册

本手册用于把数据库课程核心能力转化为可现场展示、可重复运行的证据。它不替代业务演示，而是在业务页面之外回答老师可能追问的四个问题：数据库结构是否完整、跨表数据是否一致、索引是否真正生效、并发事务是否安全。

## 1. 证据文件

| 文件 | 用途 | 是否修改业务数据 |
|---|---|---|
| `database/verify_defense.sql` | 验证 45 张表、约束、索引、对象状态及 14 类跨表业务一致性 | 否 |
| `database/defense/explain_plans.sql` | 展示订单、市场和讨论查询的 Oracle 执行计划 | 仅写 `PLAN_TABLE`，结尾回滚 |
| `database/defense/lock_session_a.sql` | 会话 A 对 Alice 钱包行持锁 8 秒 | 否，结尾回滚 |
| `database/defense/lock_session_b.sql` | 会话 B 最多等待 2 秒并验证被行锁阻塞 | 否，结尾回滚 |

所有脚本必须使用项目应用数据库用户运行。不得使用真实密码作为命令参数、脚本文本或答辩截图内容。

## 2. 完整数据库验收

在已经连接到项目 Oracle Schema 的 SQL*Plus 会话中运行：

```sql
@database/verify_defense.sql
```

成功标志是末尾出现：

```text
Database defense verification passed.
```

脚本验证以下内容：

1. 27 张核心业务表、15 张社交社区增强表和 3 张恢复审计表全部存在。
2. 45 张表全部拥有主键，业务约束均启用，索引和数据库对象均有效。
3. `PLAYER.wallet_balance` 不存在，钱包真相仍只有 `WALLET_ACCOUNT`。
4. 订单总额等于明细应付金额，支付金额与订单金额一致。
5. 退款主表金额等于退款明细金额，已支付游戏具有正常游戏库权益。
6. 市场卖单与物品所有权、模板和锁定状态一致。
7. 钱包冻结金额与活动买单冻结金额一致。
8. 成交价格满足买卖双方报价，成交存在物品转移账本。
9. 钱包收入、扣款与冻结流水的前后余额算术正确。
10. 好友关系使用稳定的小 ID/大 ID 规范，演示恢复不存在失败运行。

任何一项失败都会通过 `RAISE_APPLICATION_ERROR` 终止 SQL*Plus，并返回失败退出码。不得删除断言来掩盖真实问题。

## 3. 执行计划说明

运行：

```sql
@database/defense/explain_plans.sql
```

固定展示三类高频查询：

| 查询 | 目标索引 | 云端实际访问方式 |
|---|---|---|
| 玩家订单历史 | `IDX_ORDER_USER_TIME` | `INDEX RANGE SCAN DESCENDING` |
| CS2 市场卖单筛选 | `IDX_MARKET_TEMPLATE_STATUS` | `INDEX RANGE SCAN` |
| DST 讨论主题时间流 | `IDX_DISCUSSION_GAME_TIME` | `INDEX RANGE SCAN DESCENDING` |

答辩讲解重点：复合索引的列顺序与查询条件顺序一致；降序扫描可以直接满足按时间倒序展示。市场计划仍可能出现一次 `SORT ORDER BY`，因为复合索引在目标价格之后还要按创建时间做第二排序键，这属于可解释的执行计划，不影响当前课程数据规模。

## 4. 双会话并发演示

准备两个已经连接到同一 Oracle Schema 的 SQL*Plus 窗口。

1. 在会话 A 运行 `@database/defense/lock_session_a.sql`。
2. 看到 `SESSION A LOCKED` 后，在 8 秒内到会话 B 运行 `@database/defense/lock_session_b.sql`。
3. 会话 B 应在最多 2 秒后输出 `PASS | SESSION B BLOCKED`。
4. Oracle 23c 当前返回 `SQLCODE=-54`；其他受支持版本可能返回 `-30006`，脚本同时接受两种行锁超时代码。
5. 会话 A 约 8 秒后输出 `SESSION A RELEASED`，两个脚本均执行 `ROLLBACK`。

答辩解释：后端购买、钱包冻结、取消挂单和市场撮合都在显式事务中使用 `SELECT ... FOR UPDATE`。同一钱包或饰品的并发请求不能同时修改资产，第二个事务必须等待或失败重试，从而防止余额超扣、重复出售和所有权错乱。

## 5. 固定答辩顺序

数据库证据建议放在 UI 主业务链之后，控制在 2 至 3 分钟：

1. 展示 E-R 图和 45 表分类，不逐表朗读。
2. 运行 `verify_defense.sql`，指出全部断言通过及 Alice/Bob 钱包计算结果。
3. 运行 `explain_plans.sql`，各指出一次索引范围扫描。
4. 根据老师时间选择是否运行双会话行锁演示；时间不足时展示已保存的验收记录并讲清原理。
5. 回到业务页面说明 Oracle 约束、C# 事务和前端流程是同一条业务链的三个层次。

## 6. 2026-08-27 腾讯云验收

1. `verify_defense.sql` 在腾讯云 Oracle 23c 容器以应用用户运行成功，21 组断言全部通过。
2. 识别到 45 张预期表、45 个主键和至少 49 个命名业务索引；禁用约束、无效索引、无效对象均为 0。
3. 订单、支付、退款、游戏库、钱包冻结、市场成交、资产转移和钱包流水一致性异常均为 0。
4. 三条复杂查询均命中设计索引；`PLAN_TABLE` 变更已回滚。
5. 双会话测试中，会话 B 得到 `SQLCODE=-54` 并按预期判定为被阻塞，会话 A/B 均成功回滚。
6. 数据库 C# 契约测试 39/39 通过；验证后再次执行完整一致性脚本，固定演示数据未发生变化。
