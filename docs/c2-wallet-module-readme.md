# C2 钱包模块 README

本文档说明 Group C 中 C2/胡知鱼负责的钱包后端模块。Group C 总体交易规则仍以 `docs/group-c-core-transaction-contract.md` 和根目录 `README.md` 为准；本文只细化钱包账户、模拟充值、资金流水、金额校验和余额查询相关内容。

## 负责范围

- 钱包账户：玩家资金唯一真相来自 `WALLET_ACCOUNT.available_balance` 与 `WALLET_ACCOUNT.frozen_balance`。
- 余额查询：返回可用余额、冻结余额、查询时计算的总余额和版本号。
- 模拟充值：课程演示用，不接入真实第三方支付。
- 钱包流水：所有余额变化必须写入 `WALLET_TRANSACTION`。
- 金额校验：充值金额必须满足数据库精度和业务上限。
- 幂等控制：重复提交同一充值请求不能重复加钱。

本阶段只完成后端能力，不实现 Vue `/account/wallet` 页面。

## 接口规范

所有钱包接口只允许 `PLAYER` 调用，当前用户从 JWT claims 获取，前端不能传入 `userId`。

### 查询钱包

```text
GET /api/wallet
```

成功响应：

```json
{
  "code": 0,
  "message": "success",
  "data": {
    "walletId": "W001",
    "userId": "P001",
    "availableBalance": 176.00,
    "frozenBalance": 0.00,
    "totalBalance": 176.00,
    "version": 1
  }
}
```

`totalBalance` 只能由 `availableBalance + frozenBalance` 查询时计算，不能恢复或依赖 `PLAYER.wallet_balance`。

### 模拟充值

```text
POST /api/wallet/recharge
```

请求体：

```json
{
  "amount": 100.00,
  "idempotencyKey": "client-generated-key"
}
```

成功响应：

```json
{
  "code": 0,
  "message": "success",
  "data": {
    "walletId": "W001",
    "transactionId": "WT...",
    "availableBalance": 276.00,
    "frozenBalance": 0.00,
    "totalBalance": 276.00
  }
}
```

金额规则：

- `amount >= 0.01`。
- `amount <= 99999.99`。
- 最多两位小数。
- 不允许 0、负数或三位及以上小数。

幂等规则：

- `idempotencyKey` 必填，trim 后长度为 `1..64`。
- 同一钱包同一 `RECHARGE` 幂等键重复提交时，直接返回已有充值结果，不再次增加余额。
- 如果同一 `idempotencyKey` 已被其他钱包或其他钱包业务流水占用，返回 `IDEMPOTENCY_CONFLICT`。
- Oracle 唯一约束冲突不能直接暴露给前端，必须转换为钱包业务错误。

### 查询钱包流水

```text
GET /api/wallet/transactions?page=1&pageSize=20
```

分页规则：

- `page` 默认 `1`，小于 1 时按 1 处理。
- `pageSize` 默认 `20`，范围限制为 `1..100`。
- 排序固定为 `create_time desc, txn_id desc`。
- 返回项包含 `idempotencyKey`，便于排查重复提交。

## 事务流程

充值必须在一个 Oracle 事务中完成：

```text
校验 PLAYER 身份
  -> 校验 amount
  -> 校验 idempotencyKey
  -> select WALLET_ACCOUNT ... for update 锁定当前玩家钱包
  -> 查询 WALLET_TRANSACTION 全表幂等键
  -> 重复同一充值则直接返回已有结果
  -> 幂等键被其他业务占用则返回 IDEMPOTENCY_CONFLICT
  -> 更新 WALLET_ACCOUNT.available_balance
  -> WALLET_ACCOUNT.version + 1
  -> 写 WALLET_TRANSACTION
  -> 提交事务
```

充值流水字段要求：

- `biz_type = RECHARGE`
- `funds_direction = CREDIT`
- `biz_ref_id = transactionId`
- `amount = 本次充值金额`
- `avail_bal_before = 充值前可用余额`
- `avail_bal_after = 充值后可用余额`
- `idempotency_key = 请求幂等键`

玩家注册时必须同事务插入 `PLAYER` 和初始 `WALLET_ACCOUNT`，初始余额为 `0.00 / 0.00`。

## 错误响应

钱包接口业务错误统一返回：

```json
{
  "code": "INVALID_AMOUNT",
  "message": "Recharge amount must be between 0.01 and 99999.99.",
  "data": null
}
```

当前钱包模块错误码：

- `WALLET_NOT_FOUND`：当前玩家钱包不存在。
- `INVALID_AMOUNT`：充值金额不符合范围或小数位规则。
- `IDEMPOTENCY_KEY_REQUIRED`：幂等键为空。
- `IDEMPOTENCY_CONFLICT`：幂等键超过长度，或已被其他钱包/业务流水占用。

鉴权错误仍由统一认证守卫返回 `401` 或 `403`。

## 边界说明

- 马祥珲负责 Group C 核心交易总设计、购买事务和集成；钱包模块必须服从 Group C 总契约。
- 徐京负责退款、CDKey、游戏库等链路；退款入账仍复用 `WALLET_ACCOUNT` 和 `WALLET_TRANSACTION`。
- Group D 市场交易如涉及钱包冻结、解冻、清算，必须复用本钱包表，不得新增余额字段。
- 免费游戏 `GAME_CS2` 入库不能扣钱包余额。
- `GAME_DST` 购买、退款、CDKey 兑换是 Group C 主演示链路，钱包模块为其提供余额和流水基础。

## 验证步骤

后端测试：

```powershell
dotnet test backend\SteamPlatform.sln
```

数据库契约测试：

```powershell
dotnet test tests\SteamPlatform.Database.Tests\SteamPlatform.Database.Tests.csproj
```

建议本地 API smoke test：

1. 注册新玩家。
2. 调用 `GET /api/wallet`，确认初始余额为 0。
3. 调用 `POST /api/wallet/recharge` 充值。
4. 使用相同 `idempotencyKey` 再次充值，确认余额不重复增加。
5. 调用 `GET /api/wallet/transactions?page=1&pageSize=20`，确认存在 `RECHARGE/CREDIT` 流水且余额快照正确。
